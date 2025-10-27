# ConnectionFinder

ConnectionFinder is a module that given an ImageGraph (a result of Thinning module), will try to find connections that are missing and add them. ConnectionFinder consists of two parts:
 1) Finding possible connections for all road-ends
 2) A classifier that filters out connections that dont look good base on the image, shape of the connection and other features.

## Data generation
Training data is selected from all USA states and SA-5 countries (Colombia, Chile, Ecuador, Venezuela, Peru). Each predicted quadkey from those regions is assigned a number from 0 to 255 - an average "color" of the predicted image. This number is divided by 10 and rounded to the nearest integer - X. Then for each reagion and for each X we select an image randomly and get the dataset this way.

We run first stage of the ConnectionFinder over the dataset to get all possible connections and keep 3 random connections per quadkey. We generated an image and features per connection, select random 500 items and labeled them.

## Training a model
A classifier used is a FastTree. First we use cross-validation (with 5 folds) to identify best parameters of the FastTree model:
- tr=FastTreeBinaryClassification{lr=0.25 shrk=1.5 tdrop=0.2 nl=36 mil=25 iter=50}
- LearningRate=0.25,
- Shrinkage=1.5
- DropoutRate=0.2
- NumberOfLeaves=36
- MinDocumentsPerLeaf=25
- NumberOfTrees=50

After we found these parameters, we split the dataset into training set (60%), validation set (20%) and test set(20%). Run training on training set and run the model over validation and test sets. Then we project PR-curves and select a threshold.
<table>
<tr><td>Dataset</td><td>Precision</td><td>Recall</td></tr>
<tr><td>Validation</td><td>96.1%</td><td>61.0%</td></tr>
<tr><td>Test</td><td>93.3%</td><td>62.8%</td></tr>
</table>

current dataset is stored in ConnectionFinderRework folder in onedrive