import os
import pickle

import tensorflow as tf

import keras
from keras import backend as K
from keras.callbacks import History, ModelCheckpoint, CSVLogger, Callback
from keras.models import Model, model_from_json
from keras.utils import multi_gpu_model
from keras.optimizers import Nadam, Adam

import unet
import utils
import experiment as exp

def main(args):
    print('Set input image size: ', args.input_size)
    unet.img_rows = args.input_size
    unet.img_cols = args.input_size
    unet.num_channels = sum(args.input_channels)

    if K.image_data_format() != 'channels_first':
        raise Exception('channels first image dim ordering expected')

    model_name = args.model_name[0]
    dataset_name = args.dataset_name
    batch_size = args.batch_size
    epochs = args.epochs
    steps_per_epoch = args.steps_per_epoch
    learning_rate = args.learning_rate
    load_all_training = args.load_all_training

    print('tensorflow version:', tf.__version__)
    print('keras version:', keras.__version__)

    print('SCRIPT PARAMETERS:')
    print('--input-previous-model-path', args.previous_model_dir)
    print('--input-training-data-path', args.training_data_dir)
    print('--input-validation-data-path', args.validation_data_dir)
    print('--training-data-file', args.training_data_file)
    print('--validation-data-file', args.validation_data_file)
    print('--output-model-path', args.output_model_dir)

    print('TRAINING PARAMETERS:')
    print('architecture:', model_name)
    print('dataset:', dataset_name)
    print('batch size:', batch_size)
    print('epochs:', epochs)
    print('steps per epoch:', steps_per_epoch)
    print('learning rate:', learning_rate)
    print('load all training:', load_all_training)
    print('random seed:', args.random_seed)

    print('ENVIRONMENT VARIABLES:')
    philly_cluster = None
    cuda_visible_devices = None
    if 'PHILLY_CLUSTER' in os.environ:
        philly_cluster =  os.environ['PHILLY_CLUSTER']
        print('PHILLY_CLUSTER:', philly_cluster)
    if 'CUDA_VISIBLE_DEVICES' in os.environ:
        cuda_visible_devices = os.environ['CUDA_VISIBLE_DEVICES']
        print('CUDA_VISIBLE_DEVICES:', cuda_visible_devices)

    print('memory used/total:', utils.memory_used_and_total())

    print('creating model...')
    model = unet.get_net(model_name, learning_rate, args)

    if args.weights_file is not None:
        # to continue training the hardcoded model_name must be used, so don't load model from a file as in case of prediction
        print('CONTINUE TRAINING, {0} epochs'.format(epochs))
        print('loading weights, file: {0}...'.format(args.weights_file))
        #model.load_weights(os.path.join(args.previous_model_dir, args.weights_file[0]))
        utils.load_pickle_weights(model, os.path.join(args.previous_model_dir, args.weights_file[0]))
    else:
        print('TRAINING FROM SCRATCH, {0} epochs'.format(epochs))

    print('saving model architecture...')
    if not os.path.isdir(args.output_model_dir):
        os.makedirs(args.output_model_dir)
        print('output model directory created:', args.output_model_dir)
    with tf.gfile.Open(os.path.join(args.output_model_dir, model_name + '.json'), 'w') as f:
        f.write(model.to_json())

    print('loading training data...')
    training_data_full_path = os.path.join(args.training_data_dir, args.training_data_file)
    training_data_lines_or_offsets = [] # stores actual lines if all data are loaded into memory (load_all_training==True), otherwise offset for each line
    if load_all_training:
        training_data_lines_or_offsets = utils.load_training_data(training_data_full_path)
        print('training data: {0} rows'.format(len(training_data_lines_or_offsets)))
    else:
        training_data_lines_or_offsets.append(0)
        with tf.gfile.Open(training_data_full_path) as f:
            for line in f:
                training_data_lines_or_offsets.append(f.tell())
        print('training data: {0} rows'.format(len(training_data_lines_or_offsets)-1))

    print('loading validation data...')
    x_val, y_val = utils.load_validation_data(os.path.join(args.validation_data_dir, args.validation_data_file), args.input_channels, image_size=(args.input_size, args.input_size))
    print('validation data: {0} images {1} x {2}'.format(x_val.shape[0], x_val.shape[2], x_val.shape[3]))
    validation_data = (x_val, y_val)

    try:
        if args.gpu_num is not None:
            print('Updating model for multiple GPUs.')
            parallel_model = multi_gpu_model(model, gpus = args.gpu_num)
            parallel_model.compile(optimizer=Nadam(lr=learning_rate), loss='binary_crossentropy', metrics=['accuracy'])
            print('Updated model for multiple GPUs.')
        else:
            parallel_model = model
            print('Single GPU mode.')
    except:
        parallel_model = model
        pass

    #set up checkpoint and metrics
    print('setting up checkpoint and metrics...')
    checkpoint = exp.PickleCheckpoint(model, filepath=os.path.join(args.output_model_dir, dataset_name + '_cp-{epoch:d}_{val_loss:.4f}_{val_acc:.4f}.pkl'), monitor='loss')
    history = History()
    #reduce_lr = ReduceLROnPlateau(monitor='val_loss', factor=0.1, patience=10, min_lr=learning_rate*0.001, verbose=1)
    csv_logger = CSVLogger(os.path.join(args.output_model_dir, 'csv_logger.txt'), separator='\t')
    metrics = None
    metrics = exp.Metrics(None if args.no_checkpoint else validation_data, os.path.join(args.output_model_dir, 'metrics.txt'))
    callbacks = [checkpoint, history, csv_logger, metrics] #, reduce_lr]

    print('training model...')
    parallel_model.fit_generator(exp.batch_generator(training_data_full_path, load_all_training, training_data_lines_or_offsets, batch_size, args.input_channels, image_size=(args.train_size, args.train_size), horizontal_flip=True, vertical_flip=True, swap_axis=True, seed=args.random_seed),
                    epochs=epochs,
                    steps_per_epoch=steps_per_epoch,
                    verbose=1, #(1 if philly_cluster is None else 2), # 0 - silent, 1 - progress bar, 2 - one line per epoch
                    callbacks=callbacks,
                    validation_data=validation_data)

    pickle_weights_path = os.path.join(args.output_model_dir, model_name + '_Final.pkl')

    #save pickle model
    utils.save_pickle_weights(model, pickle_weights_path)

    #load pickle model for testing
    if args.test_loading:
        del model #save memory
        model = unet.get_net(model_name, learning_rate, args)
        utils.load_pickle_weights(model, pickle_weights_path)

    print('training complete')

if __name__ == '__main__':
    parser = utils.get_arg_parser('train script')
    args = parser.parse_args()
    if args.training_data_dir is None or args.dataset_name is None or args.training_data_file is None or args.validation_data_dir is None or args.validation_data_file is None or args.output_model_dir is None:
        parser.print_help()
    else:
        main(args)

