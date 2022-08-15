Introduction
-------------------
Bing Maps is releasing mined roads around the world. We have detected <b>37.2M km</b> of all roads and 1165K km of roads missing from OSM. Mining is performed with Bing Maps imagery between 2020 and 2022 including Maxar and Airbus. The data is freely available for download and use under the [Open Data Commons Open Database License (ODbL)](https://opendatacommons.org/licenses/odbl/).

## Data

![Mining status](images/mining-status.png)

<table>
    <thead>
        <tr>
			<th colspan=1, rowspan=2>Date</th>
            <th colspan=2>Full drop of all mined roads</th>
            <th colspan=2>OSM missing roads</th>
        </tr>
		<tr>
            <th>Region</th> <th>Length km</th>
			<th>Region</th> <th>Length km</th>
        </tr>
    </thead>
    <tbody>
		<tr>
			<td>May-2020</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/full-roads-set-model25feb2020-geo15oct2019/USA.zip">United States</a></td>
			<td>9308K</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/origUSA-PreMerge.zip">United States</a></td>
			<td>817761</td>
		</tr>
		<tr>
			<td>March-2021</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/SouthAmerica-Full.zip">South America</a></td>
			<td>4480K</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/SouthAmerica-PreMerge.zip">South America</a></td>
			<td>97850</td>
		</tr>
		<tr>
			<td>January-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/CaribbeanIslands-Full.zip">Caribbean Islands</a></td>
			<td>232K</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/CaribbeanIslands-PreMerge.zip">Caribbean Islands</a></td>
			<td>4908</td>
		</tr>
		<tr>
			<td>April-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/MiddleEast-Full.zip">Middle East</a></td>
			<td>3444K</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/MiddleEast-PreMerge.zip">Middle East</a></td>
			<td>83781</td>
		</tr>
		<tr>
			<td>April-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AsiaCenter-Full.zip">Central Asia</a></td>
			<td>1204K</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AsiaCenter-PreMerge.zip">Central Asia</a></td>
			<td>27788</td>
		</tr>
		<tr>
			<td>April-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaNorth-Full.zip">Northern Africa</a></td>
			<td>1077K</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaNorth-PreMerge.zip">Northern Africa</a></td>
			<td>24207</td>
		</tr>
		<tr>
			<td>April-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaWest-Full.zip">Western Africa</a></td>
			<td>982K</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaWest-PreMerge.zip">Western Africa</a></td>
			<td>31566</td>
		</tr>
		<tr>
			<td>April-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaCenter-Full.zip">Central Africa</a></td>
			<td>324K</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaCenter-PreMerge.zip">Central Africa</a></td>
			<td>6032</td>
		</tr>
		<tr>
			<td>April-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaEast-Full.zip">Eastern Africa</a></td>
			<td>1151K</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaEast-PreMerge.zip">Eastern Africa</a></td>
			<td>30971</td>
		</tr>
		<tr>
			<td>April-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaSouth-Full.zip">Southern Africa</a></td>
			<td>1506K</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaSouth-PreMerge.zip">Southern Africa</a></td>
			<td>39755</td>
		</tr>
		<tr>
			<td>June-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/Europe-Full.zip">Europe</a></td>
			<td>10212K</td>
			<td>N/A</td>
			<td>N/A</td>
		</tr>
		<tr>
			<td>July-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/Oceania-Full.zip">Oceania</a></td>
			<td>1947K</td>
			<td>N/A</td>
			<td>N/A</td>
		</tr>
		<tr>
			<td>July-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AmericaCenter-Full.zip">Central America</a></td>
			<td>1375K</td>
			<td>N/A</td>
			<td>N/A</td>
		</tr>
	</tbody>
</table>

## FAQ

#### What is the GeoJson format?
GeoJSON is a format for encoding a variety of geographic data structures. 
For Intensive Documentation and Tutorials, Refer to [GeoJson Blog](http://geojson.org/)

#### Data generation details:
The road extraction is done in four stages (full drop went through two stages and OSM missing set went through all four):
1.	Semantic Segmentation – Recognizing road pixels on the aerial image using Convolutional Neural Network (CNN).
2.	Geometry Generation - A series of algorithms and processes transforming output of semantic segmentation into roads in geometry format.
    - Image postprocessing
    - Thinning
    - Connectivity improvement
    - Graph construction
    - Finalizing road shapes and network quality
    - Stiching road geojsons between neighboring images where needed
3.  Conflation & Cutting - Excluding roads and parts of roads that already exist in the road network (OSM).
4.  Classification - A classifier to filter out low-confidence roads and predict a road type.

![](/images/scheme.png)

#### CNN architecture and dataset
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
The vintage of the roads depends on the vintage of the underlying imagery. Because Bing Imagery is a composite of multiple sources it is difficult to know the exact dates for individual pieces of data.

#### How good is the data?
The Osm Missing Data went through a final classifier to ensure that the precision is at least 95% (90% for USA now - to be updated to 95% in 2022). After classifier filters out potentially bad roads we remeasure the precision and make sure that it is 95% before releasing results

#### Will there be more data coming for other geographies?
Yes, we are working on processing whole world by October-2022

#### Why is the data being released?
Microsoft has a continued interest in supporting a thriving OpenStreetMap ecosystem.

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
