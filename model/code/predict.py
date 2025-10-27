import os
import pickle
import numpy as np
from bisect import bisect_left
from time import time
from statistics import mean

import tensorflow as tf
import keras
from keras import backend as K
from keras.models import Model, model_from_json

import unet
import utils

def save_prediction(y, labels, quadkeys, quadkey_stream, output_stream, output_path, is_stream, get_metrics, threshold, all_labels, all_pred):
    for i in range(len(y)):
        if threshold is None: # use default threshold 0.5
            y_pred = np.round(y[i], decimals=0)
        else:
            y_pred = y[i].copy()
            y_pred[y_pred<threshold] = 0
            y_pred[y_pred>=threshold] = 1
        # output to stream
        output_stream.write(b'%s\t%s\t%s\n' % (quadkeys[i], utils.to_base64(utils.to_byte_stream(y[i], grayscale=True, scale = False)), utils.to_base64(utils.to_byte_stream(y_pred))))
        quadkey_stream.write(quadkeys[i].decode() + '\n')
    if get_metrics:
        if threshold is None:
            y_all = np.round(y, decimals=0)
        else:
            y_all = y.copy()
            y_all[y_all<threshold] = 0
            y_all[y_all>=threshold] = 1
        if all_labels is None:
            all_labels = labels.flatten()
            all_pred = y_all.flatten()
        else:
            all_labels = np.concatenate((all_labels, labels.flatten()), axis=0)
            all_pred = np.concatenate((all_pred, y_all.flatten()), axis=0)
    return all_labels, all_pred

def process_batch(models, prediction_weights, images, labels, quadkeys, quadkey_stream, output_stream, output_path, is_stream, get_metrics, threshold, all_labels, all_pred):
    print('processing batch...')
    start_time = time()
    y = models[0].predict(images, verbose=0) # y - raw prediction
    if len(models) > 1:
        cur_weight = 1
        if prediction_weights is not None:
            cur_weight = prediction_weights[0]
        y = y * cur_weight
        for i_model in range(1, len(models)):
            if prediction_weights is not None:
                cur_weight = prediction_weights[i_model]
            y = y + (models[i_model].predict(images, verbose=0) * cur_weight) # y - raw prediction
        y[y>1]=1
    print('prediction time:', utils.elapsed_time(start_time, time()))
    all_labels, all_pred = save_prediction(y, labels, quadkeys, quadkey_stream, output_stream, output_path, is_stream, get_metrics, threshold, all_labels, all_pred)
    return all_labels, all_pred

def main(args):
    print('Set input image size: ', args.input_size)
    unet.img_rows = args.input_size
    unet.img_cols = args.input_size
    if K.image_data_format() != 'channels_first':
        raise Exception('channels first image dim ordering expected')

    print('TensorFlow version:', tf.__version__);
    print('keras version:', keras.__version__)

    print('PARAMETERS:')
    print('model dir:', args.previous_model_dir)
    print('model names:', args.model_name)
    print('model weights files:', args.weights_file)
    print('model prediction weights:', args.prediction_weights)
    print('batch size:', args.batch_size)
    print('input file:', args.input_file)
    print('output file:', args.output_file)
    print('is stream: ' + str(args.is_stream))
    print('test loading: ' + str(args.test_loading))

    start_time = time()
    batch_size = args.batch_size
    raw_output = False

    print('loading models...')
    models = []
    for i_model in range(len(args.model_name)):
        model = unet.get_net(args.model_name[i_model], 1e-3, args)
        #model = model_from_json(open(os.path.join(args.previous_model_dir,  args.model_name[i_model] + '.json')).read())
        utils.load_pickle_weights(model, os.path.join(args.previous_model_dir, args.weights_file[i_model]))
        models.append(model)

    # todo:
    # - create output streams not greater than a specified size i.e. generate several streams for every bucket
    # - use try-finally to close all open files
    print('creating data structures...')
    output_path = args.output_model_dir
    output_stream_file = os.path.join(output_path, 'pred.txt') # do not use timestamp prefix if outputing to model folder
    if not os.path.exists(output_path):
        os.makedirs(output_path)

    print('output stream file ' + ':', output_stream_file)
    images = unet.create_batch_of_images(batch_size)
    quadkeys = [''] * batch_size  # empty quadkeys
    labels = unet.create_batch_of_labels(batch_size) # only used when args.get_metrics is True
    all_labels = None # only used when args.get_metrics is True
    all_pred = None # only used when args.get_metrics is True

    print('processing road images...')

    input_stream_file = os.path.join(args.input_dir, args.input_file)
    print('input stream:', input_stream_file)

    with tf.gfile.Open(input_stream_file, 'rb') as input_stream, tf.gfile.Open(os.path.join(output_path, 'quadkeys.txt'), 'w') as quadkey_stream, tf.gfile.Open(output_stream_file, 'wb') as output_stream:
        i = 0
        k = 0
        run = 0
        for line in input_stream:
            values = line.rstrip().split(b'\t')
            q = values[0]
            encoded_image = values[1]
            images[i], quadkeys[i] = unet.load_image(utils.to_byte_stream(encoded_image)), q
            if args.get_metrics:
                encoded_label = values[2]
                labels[i] =  unet.load_label(utils.to_byte_stream(encoded_label))
            i += 1
            k += 1
            if i == len(images):
                all_labels, all_pred = process_batch(models, args.prediction_weights, images, labels, quadkeys, quadkey_stream, output_stream, output_path, args.is_stream, args.get_metrics, args.threshold, all_labels, all_pred)
                i = 0
                run += 1
                if args.batch_num is not None and args.batch_num > 0 and run >= args.batch_num:
                    break
            if k % 1000 == 0:
                print(k, 'lines read')
        # process remaining images if any exist
        if i > 0:
            all_labels, all_pred = process_batch(models,  args.prediction_weights, images[0:i], labels[0:i], quadkeys, quadkey_stream, output_stream, output_path, args.is_stream, args.get_metrics, args.threshold, all_labels, all_pred)
    if args.get_metrics:
        metrics = [[],[],[],[]]
        tp = sum((all_pred==1) & (all_labels==1)) # true positive
        tn = sum((all_pred==0) & (all_labels==0)) # true negative
        fp = sum((all_pred==1) & (all_labels==0)) # false positive
        fn = sum((all_pred==0) & (all_labels==1)) # false negative
        p = tp/(tp+fp)
        r = tp/(tp+fn)
        f1 = 2 * (p * r) / (p + r)
        acc = (tp+tn)/(tp+tn+fp+fn)
        metrics[0].append(f1)
        metrics[1].append(p)
        metrics[2].append(r)
        metrics[3].append(acc)
        with tf.gfile.Open(os.path.join(output_path, 'metrics.txt'), 'w') as metrics_file:
            metrics_file.write('f1\tprecision\trecall\taccuracy\n')
            metrics_file.write('{0}\t{1}\t{2}\t{3}\n'.format(mean(metrics[0]), mean(metrics[1]), mean(metrics[2]), mean(metrics[3])))

    print('processing complete, number of lines: {0}, time: {1}'.format(k, utils.elapsed_time(start_time, time())))

if __name__ == '__main__':
    parser = utils.get_arg_parser('process road images script')
    args = parser.parse_args()
    if args.previous_model_dir is None or args.weights_file is None or args.input_file is None or args.batch_size is None or args.model_name is None:
        parser.print_help()
        print('required parameters: --input-previous-model-path, --weights-file, --input-file, --batch-size, --model-name,')
    else:
        main(args)
