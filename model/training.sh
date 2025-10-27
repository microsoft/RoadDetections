# variables initialization
DATASET=Dataset_TYPE/Imagery_VERSION/Labels_TYPE/Labels_VERSION
TRAIN_PATH=/Datasets/"$DATASET"/train/
TRAIN_NAME=train.tsv
VALID_PATH=/Datasets/"$DATASET"/valid/
VALID_NAME=valid.tsv
BATCH_SIZE=15
INPUT_SIZE=512
EPOCHS=20
STEPS_PER_EPOCH=4000
MODEL_TYPE=res-unet-6
DATASET_NAME=HF_GG08_60

OUTPUT_PATH1=/Datasets/"$DATASET"/models/1/
OUTPUT_PATH2=/Datasets/"$DATASET"/models/2/
OUTPUT_PATH3=/Datasets/"$DATASET"/models/3/

CODE_PATH=/CodeBase/

# running models training
cd "$CODE_PATH"

python3 train.py --dataset-name "$DATASET_NAME" --input-training-data-path "$TRAIN_PATH" --training-data-file "$TRAIN_NAME" --input-validation-data-path "$VALID_PATH" --validation-data-file "$VALID_NAME" --epochs "$EPOCHS" --input-size "$INPUT_SIZE" --steps-per-epoch "$STEPS_PER_EPOCH" --batch-size "$BATCH_SIZE" --model-name "$MODEL_TYPE" --output-model-path "$OUTPUT_PATH1" --learning-rate 0.001

python3 train.py --dataset-name "$DATASET_NAME" --input-training-data-path "$TRAIN_PATH" --training-data-file "$TRAIN_NAME" --input-validation-data-path "$VALID_PATH" --validation-data-file "$VALID_NAME" --epochs "$EPOCHS" --input-size "$INPUT_SIZE" --steps-per-epoch "$STEPS_PER_EPOCH" --batch-size "$BATCH_SIZE" --model-name "$MODEL_TYPE" --output-model-path "$OUTPUT_PATH2" --input-previous-model-path "$OUTPUT_PATH1" --weights-file "$DATASET_NAME"_Final.pkl --learning-rate 0.0001

python3 train.py --dataset-name "$DATASET_NAME" --input-training-data-path "$TRAIN_PATH" --training-data-file "$TRAIN_NAME" --input-validation-data-path "$VALID_PATH" --validation-data-file "$VALID_NAME" --epochs "$EPOCHS" --input-size "$INPUT_SIZE" --steps-per-epoch "$STEPS_PER_EPOCH" --batch-size "$BATCH_SIZE" --model-name "$MODEL_TYPE" --output-model-path "$OUTPUT_PATH3" --input-previous-model-path "$OUTPUT_PATH2" --weights-file "$DATASET_NAME"_Final.pkl --learning-rate 0.00001
