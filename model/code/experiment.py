import os
import pandas as pd
from PIL import Image
import numpy as np
import glob
import platform
import math
import logging
from logging import info, warning, debug, error
from time import time, strftime, localtime
import tensorflow as tf

try:
    from tensorflow.keras.callbacks import Callback
except ImportError:
    from keras.callbacks import Callback

import unet
import utils

def batch_generator(stream_filename, load_all_training, lines_or_offsets, batch_size, input_channels, image_size=(1024, 1024), horizontal_flip=False, vertical_flip=False, swap_axis=False, seed=None):
    if seed is not None:
        np.random.seed(seed)
    while True:
        if load_all_training:
            x_batch, y_batch = unet.get_batch_using_lines(lines_or_offsets, batch_size, input_channels, image_size)
        else:
            x_batch, y_batch = unet.get_batch_using_offsets(stream_filename, lines_or_offsets, batch_size, input_channels, image_size)
        for i in range(x_batch.shape[0]):
            xb = x_batch[i]
            yb = y_batch[i]
            if horizontal_flip:
                if np.random.random() < 0.5:
                    xb = utils.flip_axis(xb, 1)
                    yb = utils.flip_axis(yb, 1)
            if vertical_flip:
                if np.random.random() < 0.5:
                    xb = utils.flip_axis(xb, 2)
                    yb = utils.flip_axis(yb, 2)
            if swap_axis:
                if np.random.random() < 0.5:
                    xb = xb.swapaxes(1, 2)
                    yb = yb.swapaxes(1, 2)
            x_batch[i] = xb
            y_batch[i] = yb
        yield x_batch, y_batch

def get_average_pr(y_pred, y_true):
    sum_precision = 0
    sum_recall = 0
    for i in range(y_pred.shape[0]):
        tp = sum(sum(sum((y_pred[i]==1) & (y_true[i]==1))))
        tn = sum(sum(sum((y_pred[i]==0) & (y_true[i]==0))))
        fp = sum(sum(sum((y_pred[i]==1) & (y_true[i]==0))))
        fn = sum(sum(sum((y_pred[i]==0) & (y_true[i]==1))))
        if tp+fp == 0:
            precision_i = 1
        else:
            precision_i = tp/(tp+fp)
        if tp+fn == 0:
            recall_i = 1
        else:
            recall_i = tp/(tp+fn)
        sum_precision += precision_i
        sum_recall += recall_i
    return sum_precision/y_pred.shape[0], sum_recall/y_pred.shape[0]


class Metrics(Callback):
    '''
    source: https://medium.com/@thongonary/how-to-compute-f1-score-for-each-epoch-in-keras-a1acd17715a2 
    parameters:
    validation_data - tuple (x_val, y_val)
    output_file [optional] - a file object
    suffix [optional] - suffix of metric names in the output, example: val_f1-2, suffix '-2'
    '''
    def __init__(self, validation_data, output_file=None, suffix=''):
        self.x_val = validation_data[0] if validation_data is not None else None
        self.y_true = validation_data[1] if validation_data is not None else None # y_val
        self.ofile = output_file
        self.suffix = suffix

    def on_train_begin(self, logs={}):
        self.f_ones = []
        self.recalls = []
        self.precisions = []
        self.accuracies = []
        header = 'epoch\tf1\tprecision\trecall\taccuracy\tavg_precision\tavg_recall\tjaccard\n'
        if os.path.exists(self.ofile):
            with tf.gfile.Open(self.ofile, 'a') as f:
                f.write(header)
        else:
            with tf.gfile.Open(self.ofile, 'w') as f:
                f.write(header)
 
    def on_epoch_end(self, epoch, logs={}):
        if self.x_val is None:
            f1 = -1.0
            p = -1.0
            r = -1.0
            acc = -1.0
        else:
            y = self.model.predict(self.x_val)
            y_pred = np.round(y, decimals=0)
            tp = sum(sum(sum(sum((y_pred==1) & (self.y_true==1))))) # true positive
            tn = sum(sum(sum(sum((y_pred==0) & (self.y_true==0))))) # true negative
            fp = sum(sum(sum(sum((y_pred==1) & (self.y_true==0))))) # false positive
            fn = sum(sum(sum(sum((y_pred==0) & (self.y_true==1))))) # false negative
            pred_cnt = sum(sum(sum(sum(y_pred==1))))
            label_cnt = sum(sum(sum(sum(self.y_true==1))))
            avg_p, avg_r = get_average_pr(y_pred, self.y_true)
            p = tp/(tp+fp)
            r = tp/(tp+fn)
            f1 = 2 * (p * r) / (p + r)
            acc = (tp+tn)/(tp+tn+fp+fn)
            jaccard = tp / (pred_cnt + label_cnt - tp)
            self.f_ones.append(f1)
            self.recalls.append(r)
            self.precisions.append(p)
            self.accuracies.append(acc)
        print('epoch: {0}, val_f1{5}: {1:.4f}, val_precision{5}: {2:.4f}, val_recall{5}: {3:.4f}, val_acc{5}: {4:.4f}'.format(epoch, f1, p, r, acc, self.suffix))
        row = '{0}\t{1:.4f}\t{2:.4f}\t{3:.4f}\t{4:.4f}\t{5:.4f}\t{6:.4f}\t{7:.4f}\n'
        if os.path.exists(self.ofile):
            with tf.gfile.Open(self.ofile, 'a') as f:
                f.write(row.format(epoch, f1, p, r, acc, avg_p, avg_r, jaccard))
        else:
            with tf.gfile.Open(self.ofile, 'w') as f:
                f.write(row.format(epoch, f1, p, r, acc, avg_p, avg_r, jaccard))

class PickleCheckpoint(Callback):
    """Save the model after every epoch.

    `filepath` can contain named formatting options,
    which will be filled the value of `epoch` and
    keys in `logs` (passed in `on_epoch_end`).

    For example: if `filepath` is `weights.{epoch:02d}-{val_loss:.2f}.hdf5`,
    then the model checkpoints will be saved with the epoch number and
    the validation loss in the filename.

    # Arguments
        filepath: string, path to save the model file.
        monitor: quantity to monitor.
        verbose: verbosity mode, 0 or 1.
        save_best_only: if `save_best_only=True`,
            the latest best model according to
            the quantity monitored will not be overwritten.
        mode: one of {auto, min, max}.
            If `save_best_only=True`, the decision
            to overwrite the current save file is made
            based on either the maximization or the
            minimization of the monitored quantity. For `val_acc`,
            this should be `max`, for `val_loss` this should
            be `min`, etc. In `auto` mode, the direction is
            automatically inferred from the name of the monitored quantity.
        save_weights_only: if True, then only the model's weights will be
            saved (`model.save_weights(filepath)`), else the full model
            is saved (`model.save(filepath)`).
        period: Interval (number of epochs) between checkpoints.
    """

    def __init__(self, model, filepath, monitor='val_loss', verbose=0,
                 save_best_only=False,
                 mode='auto', period=1):
        super(PickleCheckpoint, self).__init__()
        self.model_to_save = model
        self.monitor = monitor
        self.verbose = verbose
        self.filepath = filepath
        self.save_best_only = save_best_only
        self.period = period
        self.epochs_since_last_save = 0

        if mode not in ['auto', 'min', 'max']:
            warnings.warn('PickleCheckpoint mode %s is unknown, '
                          'fallback to auto mode.' % (mode),
                          RuntimeWarning)
            mode = 'auto'

        if mode == 'min':
            self.monitor_op = np.less
            self.best = np.Inf
        elif mode == 'max':
            self.monitor_op = np.greater
            self.best = -np.Inf
        else:
            if 'acc' in self.monitor or self.monitor.startswith('fmeasure'):
                self.monitor_op = np.greater
                self.best = -np.Inf
            else:
                self.monitor_op = np.less
                self.best = np.Inf

    def on_epoch_end(self, epoch, logs=None):
        logs = logs or {}
        self.epochs_since_last_save += 1
        if self.epochs_since_last_save >= self.period:
            self.epochs_since_last_save = 0
            filepath = self.filepath.format(epoch=epoch + 1, **logs)
            if self.save_best_only:
                current = logs.get(self.monitor)
                if current is None:
                    warnings.warn('Can save best model only with %s available, '
                                  'skipping.' % (self.monitor), RuntimeWarning)
                else:
                    if self.monitor_op(current, self.best):
                        if self.verbose > 0:
                            print('\nEpoch %05d: %s improved from %0.5f to %0.5f,'
                                  ' saving model to %s'
                                  % (epoch + 1, self.monitor, self.best,
                                     current, filepath))
                        self.best = current
                        utils.save_pickle_weights(self.model_to_save, filepath)
                    else:
                        if self.verbose > 0:
                            print('\nEpoch %05d: %s did not improve from %0.5f' %
                                  (epoch + 1, self.monitor, self.best))
            else:
                if self.verbose > 0:
                    print('\nEpoch %05d: saving model to %s' % (epoch + 1, filepath))
                utils.save_pickle_weights(self.model_to_save, filepath)

