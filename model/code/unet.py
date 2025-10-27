
import numpy as np

import io
import base64

import keras
import tensorflow as tf
from keras.models import Model, model_from_json
from keras.layers import Input, Convolution2D, MaxPooling2D, UpSampling2D, Cropping2D, Add, Reshape, Cropping2D, Cropping1D, Permute, ZeroPadding1D, ZeroPadding2D
from keras.layers import Conv2D, Conv2DTranspose, Dropout, Activation, Concatenate
from keras.layers.normalization import BatchNormalization
from keras.layers.advanced_activations import ELU
from keras import backend as K
from keras.layers.core import Lambda
from keras.optimizers import Nadam, Adam
from keras.callbacks import History, ModelCheckpoint
from keras.backend import binary_crossentropy
from keras.preprocessing.image import load_img, img_to_array, array_to_img
K.set_image_dim_ordering('th') # because dimension ordering is for theano originally, th is channel first
from PIL import Image

# these are default values. Overwritten by --input-size
img_rows = 512
img_cols = 512

num_channels = 3
num_mask_channels = 1

label_rescaling_factor = 254

def load_image(fp, grayscale=False, rescale=255):
    '''
    fp - file path or file object
    '''
    img = load_img(fp, grayscale=grayscale)
    arr = img_to_array(img)
    return arr / rescale

def load_label(fp):
    '''
    fp - file path or file object
    '''
    arr = load_image(fp, grayscale=False, rescale=label_rescaling_factor)
    if arr.shape[0] != 3 or np.count_nonzero(arr) != np.sum(arr[0]):
        raise Exception('invalid label') # R channel must have 0 or 1, G and B channels must have zeros only
    return np.delete(arr, [1,2], 0) # take R channel only

def save_image(arr, fp, scale = True):
    '''
    fp - file path or file object
    '''
    if scale:
        arr = arr * 255
        img = array_to_img(arr)
        img.save(fp, format='jpeg')
    else:
        arr = arr * 254
        img = array_to_img(arr, scale = scale)
        img.save(fp, format='jpeg')

def save_label(arr, fp):
    '''
    fp - file path or file object
    '''
    rescale = label_rescaling_factor
    if len(arr.shape) != 3 and arr.shape[0] != 1:
        raise Exception('unexpected shape of image')
    rgb_arr = np.zeros((arr.shape[1], arr.shape[2], 3), dtype=np.uint8)
    cur_arr = arr[0].copy()
    cur_arr[cur_arr>1]=1
    rgb_arr[:,:,0] = cur_arr * rescale # RGB label image
    img1 = Image.fromarray(rgb_arr)
    p_img1 = img1.convert('P', palette=Image.ADAPTIVE)
    p_img1.save(fp, format='png')

def create_batch_of_images(batch_size):
    return np.zeros((batch_size, num_channels, img_rows, img_cols))

def create_batch_of_labels(batch_size):
    return np.zeros((batch_size, num_mask_channels, img_rows, img_cols))

def get_data_for_pred_from_multi_image_row(row, input_channels, image_size):
    c = 0 #starting channel
    x = np.zeros((sum(input_channels), image_size[0], image_size[1]), dtype=np.float32)
    y = np.zeros((1, image_size[0], image_size[1]), dtype=np.float32)
    #load x (images)
    for j in range(len(input_channels)):
        if input_channels[j] != 0:
            img = load_img(to_byte_stream(row[j+1]), grayscale=False)
            arr = img_to_array(img) / 255
            x[c:c+input_channels[j],:,:] = arr[0:input_channels[j],:,:]
            c = c + input_channels[j]
    return x

def get_data_from_multi_image_row(row, input_channels, image_size): #[name, image files, label file]
    c = 0 #starting channel
    x = np.zeros((sum(input_channels), image_size[0], image_size[1]), dtype=np.float32)
    y = np.zeros((1, image_size[0], image_size[1]), dtype=np.float32)
    #load x (images)
    for j in range(len(input_channels)):
        if input_channels[j] != 0:
            #base64_bytes = base64.b64decode(row[j+1])
            #arr = np.frombuffer(base64_bytes, dtype=np.uint8).reshape(3,image_size[0],image_size[1])
            img = load_img(to_byte_stream(row[j+1]), grayscale=False)
            arr = img_to_array(img) / 255
            x[c:c+input_channels[j],:,:] = arr[0:input_channels[j],:,:]
            c = c + input_channels[j]
    #load y (label)
    #base64_bytes = base64.b64decode(row[-1])
    #arr = np.frombuffer(base64_bytes, dtype=np.uint8).reshape(3,image_size[0],image_size[1])
    img = load_img(to_byte_stream(row[-1]), grayscale=False)
    arr = img_to_array(img) / label_rescaling_factor
    if arr.shape[0] != 3 or np.count_nonzero(arr) != np.sum(arr[0]):
        raise Exception('invalid label')
    y = np.delete(arr, [1,2], 0)
    return x, y

def get_batch_using_lines(training_data, batch_size, input_channels, image_size=(1024, 1024)):
    x_batch = np.zeros((batch_size, sum(input_channels), img_rows, img_cols))
    y_batch = np.zeros((batch_size, num_mask_channels, img_rows, img_cols))
    height, width = image_size
    for i in range(batch_size):
        top = np.random.randint(0, height - img_rows)
        left = np.random.randint(0, width - img_cols)
        x, y = get_data_from_multi_image_row(training_data[np.random.randint(0, len(training_data))], input_channels, image_size)
        x_batch[i] = x[:, top: top + img_rows, left: left + img_cols]
        y_batch[i] = y[:, top: top + img_rows, left: left + img_cols]
    return x_batch, y_batch

def to_byte_stream(x, grayscale=False, scale = True):
    '''
    x - base64 encoded byte string, image array or label array
    grayscale - if x is np.ndarray and represents 1-channel image and grayscale is True (ignored in other cases)
    then x is saved as grayscale PNG image not palette-based as in case of label, default value - False
    '''
    byte_stream = None
    if isinstance(x, bytes):
        byte_stream = io.BytesIO(base64.b64decode(x))
    elif isinstance(x, str):
        byte_stream = io.BytesIO(base64.b64decode(x.encode()))
    elif isinstance(x, np.ndarray):
        if len(x.shape) != 3:
            raise Exception('unexpected shape of array: {0}'.format(x.shape))
        byte_stream = io.BytesIO()
        if x.shape[0] == 3:
            unet.save_image(x, byte_stream)
        elif x.shape[0] == 1:
            if grayscale:
                unet.save_image(x, byte_stream, scale) # x has 1 channel and values between 0.0 and 1.0
            else:
                unet.save_label(x, byte_stream) # x has 1 channel and values 0.0 or 1.0 only
        else:
            raise Exception('unexpected number of channels: {0}'.format(x.shape[0]))
        byte_stream.seek(0)
    else:
        raise Exception('unexpected type of x:', type(x))
    return byte_stream

def get_batch_using_offsets(stream_filename, line_offsets, batch_size, input_channels, image_size=(1024, 1024)):
    lines = get_random_lines_from_stream(stream_filename, line_offsets, batch_size)
    x_batch = np.zeros((batch_size, sum(input_channels), img_rows, img_cols))
    y_batch = np.zeros((batch_size, num_mask_channels, img_rows, img_cols))
    height, width = image_size
    for i in range(len(lines)):
        top = np.random.randint(0, height - img_rows)
        left = np.random.randint(0, width - img_cols)
        x, y = get_data_from_multi_image_row(lines[i], input_channels, image_size)
        x_batch[i] = x[:, top: top + img_rows, left: left + img_cols]
        y_batch[i] = y[:, top: top + img_rows, left: left + img_cols]
    return x_batch, y_batch

def get_random_lines_from_stream(stream_filename, line_offsets, batch_size):
    line_count = len(line_offsets)-1
    line_nums = np.random.randint(0, line_count, (batch_size))
    list = []
    with tf.gfile.Open(stream_filename) as f:
        for line_num in line_nums:
            f.seek(line_offsets[line_num])
            line = f.readline()
            row = line[:-1].split('\t')
            list.append(row)
    return list

def bn_act(x, act=True):
    x=BatchNormalization(axis=1)(x)
    if act == True:
        x = Activation('relu')(x)
    return x

def conv_block(x, filters, kernel_size=(3,3), padding='same', strides=1):
    conv = bn_act(x)
    conv = Conv2D(filters, kernel_size, padding=padding, kernel_initializer='he_uniform', strides=strides)(conv)
    return conv

def stem(x, filters, kernel_size=(3,3), padding='same', strides=1):
    conv = Conv2D(filters, kernel_size, padding=padding, kernel_initializer='he_uniform', strides=strides)(x)
    conv = conv_block(conv, filters, kernel_size=kernel_size, padding=padding, strides=strides)

    shortcut = Conv2D(filters, kernel_size=(1, 1), padding=padding, kernel_initializer='he_uniform', strides=strides)(x)
    shortcut = bn_act(shortcut, act=False)

    output = Add()([conv, shortcut])
    return output

def residual_block(x, filters, kernel_size=(3, 3), padding='same', strides=1):
    res = conv_block(x, filters, kernel_size=kernel_size, padding=padding, strides=strides)
    res = conv_block(res, filters, kernel_size=kernel_size, padding=padding, strides=1)

    shortcut = Conv2D(filters, kernel_size=(1, 1), padding=padding, kernel_initializer='he_uniform', strides=strides)(x)
    shortcut = bn_act(shortcut, act=False)

    output = Add()([shortcut, res])
    return output

def upsample_concat_block(x, xskip):
    u = UpSampling2D((2, 2))(x)
    c = Concatenate(axis=1)([u, xskip])
    return c

# Residual U-Net https://arxiv.org/pdf/1711.10684.pdf Projection, 14 layers. batch_size: 18
def get_res_unet6(learning_rate):
    f = [32, 64, 128, 256, 512, 1024, 2048]

    # encode
    inputs = Input((num_channels, img_rows, img_cols))
    e1 = stem(inputs, f[0])
    e2 = residual_block(e1, f[1], strides=2)
    e3 = residual_block(e2, f[2], strides=2)
    e4 = residual_block(e3, f[3], strides=2)
    e5 = residual_block(e4, f[4], strides=2)
    e6 = residual_block(e5, f[5], strides=2)

    # bridge
    b1 = conv_block(e6, f[6], strides=2)
    b2 = conv_block(b1, f[6], strides=1)

    # decode
    up1 = upsample_concat_block(b2, e6)
    d1 = residual_block(up1, f[5])
    up2 = upsample_concat_block(d1, e5)
    d2 = residual_block(up2, f[4])
    up3 = upsample_concat_block(d2, e4)
    d3 = residual_block(up3, f[3])
    up4 = upsample_concat_block(d3, e3)
    d4 = residual_block(up4, f[2])
    up5 = upsample_concat_block(d4, e2)
    d5 = residual_block(up5, f[1])
    up6 = upsample_concat_block(d5, e1)
    d6 = residual_block(up6, f[0])
    #
    outputs = Conv2D(num_mask_channels, 1, activation='sigmoid')(d6)
    #
    model = Model(inputs=inputs, outputs=outputs)
    #
    # todo: add F1 score to metrics
    model.compile(optimizer=Nadam(lr=learning_rate), loss='binary_crossentropy', metrics=['accuracy'])
    return model

def get_net(model_name, learning_rate, args = None):
    if model_name == 'res-unet-6':
        return get_res_unet6(learning_rate)
    raise Exception('unknown model name')

