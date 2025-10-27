Model file is HF_GG08_60.pkl

You can find python code that can be used to run training and/or inference in code/ directory

Training was performed in 3 stages: each for 20 epochs with 4000 steps per epoch. Dataset had images of size 1024 and during training patches of size 512 were cut out a random sample of them at each step and ran through the neural net (see training.sh for details)

Inference was performed on images of size 1088 and then padding of 32 was cut out from the result to increase quality of prediction. Later predictions were used for geometry generation (see inference.sh for details)

Keras version is quite old and requires a quite old version of python - 3.6 (see requirements.txt for dependancies)