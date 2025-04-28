Introduction
-------------------
Bing Maps is releasing mined roads around the world. We have detected <b>54.2M km</b> of roads worldwide. Mining is performed with Bing Maps imagery including Maxar and Airbus. The data is freely available for download and use under the [Open Data Commons Open Database License (ODbL)](https://opendatacommons.org/licenses/odbl/).

## Data

![Mining status](images/ARMP.Heatmap-v.2025.01.01.01.00.00.png)

<table width="100%">
    <thead>
		<tr>
			<th>Region</th>
			<th>Length in '000 Km</th>
        </tr>
    </thead>
    <tbody>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Australia_and_Oceania.zip">Australia and Oceania</a></td>
			<td>2314.7</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Caribbean.zip">Caribbean</a></td>
			<td>243.7</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Central_America.zip">Central America</a></td>
			<td>1538.3</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Central_Asia.zip">Central Asia</a></td>
			<td>1204</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Eastern_Africa.zip">Eastern Africa</a></td>
			<td>1668.8</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Eastern_Asia.zip">Eastern Asia</a></td>
			<td>153.9</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Eastern_Europe.zip">Eastern Europe</a></td>
			<td>4601.4</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Middle_Africa.zip">Middle Africa</a></td>
			<td>513.8</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Northern_Africa.zip">Northern Africa</a></td>
			<td>1387.2</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Northern_America.zip">Northern America</a></td>
			<td>12990.6</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Northern_Europe.zip">Northern Europe</a></td>
			<td>2380</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/South_America.zip">South America</a></td>
			<td>5694.7</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Southeastern_Asia.zip">Southeastern Asia</a></td>
			<td>2777</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Southern_Africa.zip">Southern Africa</a></td>
			<td>1217.9</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Southern_Asia.zip">Southern Asia</a></td>
			<td>5676.3</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Southern_Europe.zip">Southern Europe</a></td>
			<td>2727.7</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Western_Africa.zip">Western Africa</a></td>
			<td>1130.3</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Western_Asia.zip">Western Asia</a></td>
			<td>2444.4</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/Western_Europe.zip">Western Europe</a></td>
			<td>3560.5</td>
		</tr>
		<tr>
			<td><a href="https://usaminedroads.z19.web.core.windows.net/drops/2025.04.28/World.zip">World</a></td>
			<td>54225.2</td>
		</tr>
	</tbody>
</table>

## FAQ

#### What is the data format?
Each file has all roads from a certain geographical region. Each row in a file has an Alpha-3 code and a geojson of a road (Alpha-3 code of a region where the road geojson approximately is) separated with TAB (\t). Each geojson also contains property "WidthMeters" - approximate width of the road in meters.

World is divided into subregions for easier usability based on <a href="https://en.wikipedia.org/wiki/United_Nations_geoscheme">United Nations geoscheme</a>

Alpha-3 codes are used from <a href="https://www.iban.com/country-codes">IBAN</a> and <a href="https://en.wikipedia.org/wiki/ISO_3166-1_alpha-3">Wikipedia page</a>. Also refer to AlphaCodeToRegionName.tsv file (some smaller regions/disputed areas might have ambigious codes)

#### What is the GeoJson format?
GeoJSON is a format for encoding a variety of geographic data structures. 
For Intensive Documentation and Tutorials, Refer to [GeoJson Blog](http://geojson.org/)

#### Data generation details:
The road extraction is done in two major stages:
1.	Semantic Segmentation – Recognizing road pixels on the aerial image using Convolutional Neural Network (CNN).
2.	Geometry Generation - A series of algorithms and processes transforming output of semantic segmentation into roads in geometry format.
    - Image postprocessing
    - Thinning
    - Connectivity improvement
    - Graph construction
    - Finalizing road shapes and network quality
    - Stiching road geojsons between neighboring images where needed

![](/images/scheme.png)

#### Neural network architecture and dataset
Our network was based on UNet and ResNet and the following papers [U-Net] (https://arxiv.org/abs/1505.04597), [Res U-Net] (https://arxiv.org/pdf/1512.03385.pdf), [Res U-Net] (https://arxiv.org/pdf/1711.10684.pdf).
The model was trained on 512x512 images, it is fully-convolutional, which allows images of any size (that is divisable by 64) be processed by the model (constrained by GPU memory, 1088x1088 in our case). The training set consists of 20000 labeled images. Majority of the satellite images cover diverse areas all around the world. To achieve a good set representation, we have enriched the set with samples from various areas covering mountains, glaciers, forests, deserts, beaches, coasts, etc.
Images in the set are of 1088x1088 pixel size with 100 cm/pixel resolution. The training is done with Keras toolkit.

#### Metrics
We measure intermediate stage metrics to track performance of our models. <i>Pixel metric</i> measures performance of the the Convolutional Neural Network and <i><a href='https://medium.com/the-downlinq/spacenet-road-detection-and-routing-challenge-part-i-d4f59d55bfce'>APLS metric (Average Path Length Similarity)</a></i> measures overall connectivity after geometry generation stage.

| Metric        | Precision    | Recall    |
| ------------- |:-------------:|:-------------:|
|Pixel|85.24%|82.81%|
|APLS|87.53%|79.33%|

#### Data Vintage
The vintage of the roads depends on the vintage of the underlying imagery. Because Bing Imagery is a composite of multiple sources it is difficult to know the exact dates for individual pieces of data. However data is up-to-date with freshest available imagery from Microsoft Maps.

#### How good is the data?
The result of the pipeline (after going through conflation, cutting, filtering and quality control reached 95% precision and pushed into Microsoft Maps production.

#### Why is the data being released?
Microsoft has a continued interest in supporting a thriving OpenStreetMap ecosystem.

#### Next steps
We will opensource both NN model and geometry generation code in 2025

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

# Legal Notices

Microsoft, Windows, Microsoft Azure and/or other Microsoft products and services referenced in the documentation
may be either trademarks or registered trademarks of Microsoft in the United States and/or other countries.
The licenses for this project do not grant you rights to use any Microsoft names, logos, or trademarks.
Microsoft's general trademark guidelines can be found at http://go.microsoft.com/fwlink/?LinkID=254653.

Privacy information can be found at https://privacy.microsoft.com/en-us/

Microsoft and any contributors reserve all other rights, whether under their respective copyrights, patents,
or trademarks, whether by implication, estoppel or otherwise.
