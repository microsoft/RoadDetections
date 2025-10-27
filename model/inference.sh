# variables initialization
DATASET=Dataset_TYPE/Imagery_VERSION/Labels_TYPE/Labels_VERSION
BATCH_SIZE=10
INPUT_SIZE=1088
MODEL_TYPE=res-unet-6

MODEL_PATH=/Datasets/"$DATASET"/models/3/
MODEL_NAME=HF_GG08_60
DATA_PATH=/Datasets/"$DATASET"/test/
DATA_NAME=test.tsv
PRED_PATH=/Datasets/"$DATASET"/pred/test/

CODE_PATH=/CodeBase/

# running models inference
cd "$CODE_PATH"

python3 predict.py --input-dir "$DATA_PATH" --input-file "$DATA_NAME" --input-size "$INPUT_SIZE" --batch-size "$BATCH_SIZE" --weights-file "$MODEL_NAME"_Final.pkl --model-name "$MODEL_TYPE" --output-first --input-previous-model-path "$MODEL_PATH" --output-model-path "$PRED_PATH" --is-stream
