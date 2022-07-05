Introduction
-------------------
This data is freely available for download and use and contains 
1. USA
	- 54 million computer generated roads (9306K km)
	- 5.9 million computer generated roads (817K km) that are missing in OpenStreetMaps roads drop from 02-May-2020
2. South America
	- 18.28 million computer generated roads (4480K km)
	- 297.3K computer generated roads (97850 km) that are missing in OpenStreetMaps roads drop from 01-March-2021/08-February-2022
3. Caribbean Islands
	- 1.17 million computer generated roads (232K km)
	- 27.5K computer generated roads (4908 km) that are missing in OpenStreetMaps roads drop from 13-January-2022
4. Middle East
	- 17.60 million computer generated roads (3444K km)
	- 417.2K computer generated roads (83781 km) that are missing in OpenStreetMaps roads drop from 04-April-2022
5. Central Asia
	- 5.35 million computer generated roads (1204K km)
	- 123.7K computer generated roads (27788 km) that are missing in OpenStreetMaps roads drop from 04-April-2022
6. Northern Africa
	- 5.33 million computer generated roads (1077K km)
	- 115.5K computer generated roads (24207 km) that are missing in OpenStreetMaps roads drop from 20-April-2022
7. Western Africa
	- 4.44 million computer generated roads (982K km)
	- 175.3K computer generated roads (31566 km) that are missing in OpenStreetMaps roads drop from 20-April-2022
8. Central Africa
	- 1.11 million computer generated roads (324K km)
	- 28.9K computer generated roads (6032 km) that are missing in OpenStreetMaps roads drop from 20-April-2022
9. Eastern Africa
	- 4.34 million computer generated roads (1151K km)
	- 137.2K computer generated roads (30971 km) that are missing in OpenStreetMaps roads drop from 20-April-2022
10. Southern Africa
	- 5.15 million computer generated roads (1506K km)
	- 190.2K computer generated roads (39755 km) that are missing in OpenStreetMaps roads drop from 20-April-2022

License
-------------------
This data is licensed by Microsoft under the [Open Data Commons Open Database License (ODbL)](https://opendatacommons.org/licenses/odbl/)

## FAQ

#### What is the GeoJson format?
GeoJSON is a format for encoding a variety of geographic data structures. 
For Intensive Documentation and Tutorials, Refer to [GeoJson Blog](http://geojson.org/)

#### Creation Details:
The road extraction is done in four stages (first dataset went through two stages and second went through all four):
1.	Semantic Segmentation – Recognizing road pixels on the aerial image using Convolutional Neural Network (CNN).
2.	Geometry Generation - A series of algorithms and processes transforming output of semantic segmentation into roads in geometry format.
3.  Conflation & Cutting - Excluding roads and parts of roads that already exist in the road network (OSM).
4.  Classification - A classifier to filter out low-confidence roads and predict a road type.

### Scheme
![](/images/scheme.png)

#### CNN architecture
Our network was based on UNet and ResNet and the following papers [U-Net] (https://arxiv.org/abs/1505.04597), [Res U-Net] (https://arxiv.org/pdf/1512.03385.pdf), [Res U-Net] (https://arxiv.org/pdf/1711.10684.pdf).
The model was trained on 512x512 images, it is fully-convolutional, meaning that the model can be applied to an image of any size (constrained by GPU memory, 1088x1088 in our case).

#### Training details
The training set consists of 20000 labeled images. Majority of the satellite images cover diverse residential areas all around the world. For the sake of good set representation, we have enriched the set with samples from various areas covering mountains, glaciers, forests, deserts, beaches, coasts, etc.
Images in the set are of 1088x1088 pixel size with 1 meter/pixel resolution. The training is done with Keras toolkit.

#### Metrics
We measure intermediate stage metrics to track performance of our models. <i>Pixel metric</i> measures performance of the the Convolutional Neural Network and <i><a href='https://medium.com/the-downlinq/spacenet-road-detection-and-routing-challenge-part-i-d4f59d55bfce'>APLS metric (Average Path Length Similarity)</a></i> measures overall connectivity after geometry generation stage.

| Metric        | Precision    | Recall    |
| ------------- |:-------------:|:-------------:|
|Pixel|85.24%|82.81%|
|APLS|87.53%|79.33%|

#### Description
Geometry generation consists of the following steps
1. Image postprocessing
2. Thinning
3. Connectivity improvement
4. Graph construction
5. Finalizing road shapes and network quality
6. Stiching road geojsons between neighboring images where needed

#### Data Vintage
The vintage of the roads depends on the vintage of the underlying imagery. Because Bing Imagery is a composite of multiple sources it is difficult to know the exact dates for individual pieces of data.

#### How good are the data?
The Osm Missing Data went through a final classifier to ensure that the precision is at least 95% (90% for USA now - to be updated to 95% in 2022). After classifier filters out potentially bad roads we remeasure the precision and make sure that it is 95% before releasing results

#### Will there be more data coming for other geographies?
Yes, we are working on processing whole world by October-2022

#### Why is the data being released?
Microsoft has a continued interest in supporting a thriving OpenStreetMap ecosystem.

### External References

<table>
    <thead>
        <tr>
			<th colspan=1, rowspan=2>Date</th>
            <th colspan=4>Full drop of all mined roads</th>
            <th colspan=4>OSM missing roads</th>
        </tr>
		<tr>
            <th>State</th> <th>Number of Roads</th>  <th>Length km</th> <th>Unzipped MB</th>
			<th>Region</th> <th>Number of Roads</th>  <th>Length km</th> <th>Unzipped MB</th>
        </tr>
    </thead>
    <tbody>
		<tr>
			<td>02-May-2020</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/full-roads-set-model25feb2020-geo15oct2019/USA.zip">USA</a></td>
			<td>54.5M</td><td>9308K</td><td>13459</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/origUSA-PreMerge.zip">USA</a></td>
			<td>5931242</td><td>817761</td><td>1259</td>
		</tr>
		<tr>
			<td>01-March-2021/08-February-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/SouthAmerica-Full.zip">South America</a></td>
			<td>18.28M</td><td>4480K</td><td>4001</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/SouthAmerica-PreMerge.zip">South America</a></td>
			<td>297.3K</td><td>97850</td><td>54</td>
		</tr>
		<tr>
			<td>13-January-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/CaribbeanIslands-Full.zip">Caribbean Islands</a></td>
			<td>1.17M</td><td>232K</td><td>245</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/CaribbeanIslands-PreMerge.zip">Caribbean Islands</a></td>
			<td>27.5K</td><td>4908</td><td>5</td>
		</tr>
		<tr>
			<td>04-April-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/MiddleEast-Full.zip">Middle East</a></td>
			<td>17.6M</td><td>3444K</td><td>3702</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/MiddleEast-PreMerge.zip">Middle East</a></td>
			<td>417.2K</td><td>83781</td><td>70</td>
		</tr>
		<tr>
			<td>04-April-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AsiaCenter-Full.zip">Central Asia</a></td>
			<td>5.35M</td><td>1204K</td><td>1128</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AsiaCenter-PreMerge.zip">Central Asia</a></td>
			<td>123.7K</td><td>27788</td><td>21</td>
		</tr>
		<tr>
			<td>20-April-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaNorth-Full.zip">Northern Africa</a></td>
			<td>5.33M</td><td>1077K</td><td>1110</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaNorth-PreMerge.zip">Northern Africa</a></td>
			<td>115.5K</td><td>24207</td><td>19</td>
		</tr>
		<tr>
			<td>20-April-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaWest-Full.zip">Western Africa</a></td>
			<td>4.44M</td><td>982K</td><td>915</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaWest-PreMerge.zip">Western Africa</a></td>
			<td>175.3K</td><td>31566</td><td>27</td>
		</tr>
		<tr>
			<td>20-April-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaCenter-Full.zip">Central Africa</a></td>
			<td>1.11M</td><td>324K</td><td>255</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaCenter-PreMerge.zip">Central Africa</a></td>
			<td>28.9K</td><td>6032</td><td>5</td>
		</tr>
		<tr>
			<td>20-April-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaEast-Full.zip">Eastern Africa</a></td>
			<td>4.34M</td><td>1151K</td><td>972</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaEast-PreMerge.zip">Eastern Africa</a></td>
			<td>137.2K</td><td>30971</td><td>23</td>
		</tr>
		<tr>
			<td>20-April-2022</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaSouth-Full.zip">Southern Africa</a></td>
			<td>5.15M</td><td>1506K</td><td>1120</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/road-detections/AfricaSouth-PreMerge.zip">Southern Africa</a></td>
			<td>190.2K</td><td>39755</td><td>31</td>
		</tr>
	</tbody>
</table>
<br>
<br>

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
