import os
import pickle
import glob
import shutil
import random 
import numpy as np
import math
import cv2
import json
from PIL import Image, ImageDraw
import pandas as pd
import argparse
import base64
import io
from time import time, strftime, localtime
import psutil
import requests
import gzip
import io
import tensorflow as tf

import keras
from keras import backend as K
from keras.preprocessing.image import load_img, img_to_array

import unet

LATLON_EPS = 0.00001 # 0.0001 degrees -> 11.13 meters at 0 latitude, so distance for LATLON_EPS is ~1.1 meter or less

def datetime_suffix(t):
    return strftime('_%Y_%m_%d_%H', localtime(t))

def elapsed_time(start_time, end_time):
    diff = end_time - start_time
    return '{0:.0f}:{1:.0f}:{2:.3f}'.format(int(diff) // 3600, int(diff) % 3600 // 60, diff % 60)

def number_of_bytes_to_string(n):
    sfx = ['', 'K', 'M', 'G', 'T']
    i = 0
    while i < 4 and n > 512: n /= 1024; i += 1
    return '{0:.1f}{1}'.format(n, sfx[i])

def memory_used_and_total():
    mem = psutil.virtual_memory()
    return number_of_bytes_to_string(mem.used) + '/' + number_of_bytes_to_string(mem.total)

def get_arg_parser(description='generic argument parser'):
    parser = argparse.ArgumentParser(description=description)
    parser.add_argument('--input-dir', dest='input_dir', help='input directory')
    parser.add_argument('--input-file', dest='input_file', help='input file to be read from an input directory')
    parser.add_argument('--output-dir', dest='output_dir', help='output directory used to write small files')
    parser.add_argument('--output-file', dest='output_file', help='output file to be written to an output directory')
    parser.add_argument('--model-file', dest='model_file', help='saved architecture of model')
    parser.add_argument('--weights-file', dest='weights_file', nargs='+', help='saved weights of model')
    parser.add_argument('--dataset-name', dest='dataset_name', help='name of training dataset, it is used for names of checkpoints')
    parser.add_argument('--training-data-file', dest='training_data_file', help='training data file')
    parser.add_argument('--validation-data-file', dest='validation_data_file', nargs='+', help='validation data file')
    # training parameters
    parser.add_argument('--learning-rate', dest='learning_rate', help='learning rate to train a model from scratch: 0.001, to continue training: 0.0001', type=float, default=1e-3)
    parser.add_argument('--batch-size', dest='batch_size', help='batch size', type=int, default=32)
    parser.add_argument('--epochs', dest='epochs', help='number of epochs to train from scratch: 50, to continue training: 30', type=int, default=50)
    parser.add_argument('--steps-per-epoch', dest='steps_per_epoch', help='2000 steps per epoch covers 4K big images: 4K * 16 = 64K -> 32 batch size * 2000 = 64K', type=int, default=2000)
    parser.add_argument('--random-seed', dest='random_seed', help='random seed value', type=int)
    # parameters for '[Philly] Run TensorFlow Job' module
    parser.add_argument('--input-previous-model-path', dest='previous_model_dir', help='')
    parser.add_argument('--input-training-data-path', dest='training_data_dir', help='it always must be a directory not a file, file name should be specified in --training-data')
    parser.add_argument('--input-validation-data-path', dest='validation_data_dir', help='it always must be a directory not a file, file name should be specified in --validation-data')
    parser.add_argument('--output-model-path', dest='output_model_dir', help='all model related stuff, checkpoints and metrics per epoch for example')
    parser.add_argument('--model-name', dest='model_name', nargs='+', help='model name for triaining', default='res-unet-6')
    parser.add_argument('--batch-num', dest='batch_num', help='number of batches for prediction', type=int, default=-1)
    parser.add_argument('--test-loading', dest='test_loading', help='load the model after training for testing', action='store_true')
    parser.add_argument('--is-stream', dest='is_stream', help='whether to output prediction to stream', action='store_true')
    parser.add_argument('--get-metrics', dest='get_metrics', help='prediction: whether to calculate metrics, only works when input data contains labels', action='store_true')
    parser.add_argument('--no-checkpoint', dest='no_checkpoint', help='training: do not evaluation model at checkoutpoint to save memory', action='store_true')
    parser.add_argument('--gpu-num', dest='gpu_num', help='number of gpus to use in prediction stage', type=int, default=1)
    parser.add_argument('--threshold', dest='threshold', help='Threshold used for calculating metrics and prediction.', type=float)
    parser.add_argument('--prediction-weights', dest='prediction_weights', nargs='+', help='Weights for each prediciton. Only used when there are multiple model files.', type=float)
    parser.add_argument('--input-size', dest='input_size', help='image size for training or prediction', type=int, default=256)
    parser.add_argument('--train-size', dest='train_size', help='orignal image size in training stream used for sampling', type=int, default=1024)
    parser.add_argument('--input-channels', dest='input_channels', nargs='+', help='list of channel numbers for input images used in train.py. 0 means do not use correponding image', type=int)
    parser.add_argument('--load-all-training', dest='load_all_training', help='whether to load all training data into memory for train_mi', action='store_true')
    return parser

def to_base64(x):
    '''
    x - file path, sequence of bytes or image array
    '''
    s = None
    if isinstance(x, str):
        if not os.path.isfile(x):
            raise Exception('invalid file path:'+ x)
        with tf.gfile.Open(x, 'rb') as f:
            s = f.read()
    elif isinstance(x, bytes):
        s = x
    elif isinstance(x, io.BytesIO):
        s = x.read()
    else:
        raise Exception('not a file path')
    return base64.b64encode(s)

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

def flip_axis(x, axis):
    x = np.asarray(x).swapaxes(axis, 0)
    x = x[::-1, ...]
    x = x.swapaxes(0, axis)
    return x

def load_validation_data(stream_file, input_channels, image_size=(256, 256)):
    start_time = time()
    with tf.gfile.Open(stream_file) as f:
        n = len(f.readlines())
    x = np.zeros((n, sum(input_channels), image_size[0], image_size[1]), dtype=np.float32)
    y = np.zeros((n, 1, image_size[0], image_size[1]), dtype=np.float32)
    with tf.gfile.Open(stream_file) as f:
        i = 0
        for line in f:
            row = line[:-1].split('\t')
            x[i], y[i] = unet.get_data_from_multi_image_row(row, input_channels, image_size)
            i += 1
    print('{0} images/labels loaded, time: {1}, memory: {2}'.format(i, elapsed_time(start_time, time()), memory_used_and_total()))
    return (x, y)

def load_training_data(stream_file):
    start_time = time()
    list = []
    with tf.gfile.Open(stream_file) as f:
        i = 0
        for line in f:
            row = line[:-1].split('\t')
            list.append(row)
            i += 1
    print('{0} lines loaded, time: {1}, memory: {2}'.format(i, elapsed_time(start_time, time()), memory_used_and_total()))
    return list

def save_pickle_weights(model, filepath):
    weights = model.get_weights()
    with tf.gfile.Open(filepath, 'wb') as f:
        pickle.dump(weights, f, protocol = pickle.HIGHEST_PROTOCOL)
        print('pickle model saved at:' + filepath)

def load_pickle_weights(model, filepath):
    with tf.gfile.Open(filepath, 'rb') as f:
        weights = pickle.load(f)
        model.set_weights(weights)
        print('pickle model loaded from:' + filepath)

if K.image_data_format() != 'channels_first':
    raise Exception('channels first image dim ordering expected')

