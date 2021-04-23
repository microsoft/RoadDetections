Introduction
-------------------
This data is freely available for download and use and contains 
1. USA
	- 54.5 million computer generated roads (9308k km)
	- 5.9 million computer generated roads (817k km) that are missing in OpenStreetMaps roads drop from 02-May-2020
2. Chile
	- 1.05 million computer generated roads (170k km)
	- 22,525 computer generated roads (4,461 km) that are missing in OpenStreetMaps roads drop from 01-March-2021
3. Colombia
	- 0.85 million computer generated roads (140k km)
	- 10,241 computer generated roads (1,928 km) that are missing in OpenStreetMaps roads drop from 01-March-2021
4. Ecuador
	- 0.56 million computer generated roads (93k km)
	- 10,863 computer generated roads (1,752 km) that are missing in OpenStreetMaps roads drop from 01-March-2021
5. Peru
	- 0.73 million computer generated roads (109k km)
	- 5,575 computer generated roads (927 km) that are missing in OpenStreetMaps roads drop from 01-March-2021
6. Venezuela
	- 0.91 million computer generated roads (162k km)
	- 20,434 computer generated roads (4,631 km) that are missing in OpenStreetMaps roads drop from 01-March-2021

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
The training set consists of 16800 labeled images. Majority of the satellite images cover diverse residential areas in the US. For the sake of good set representation, we have enriched the set with samples from various areas covering mountains, glaciers, forests, deserts, beaches, coasts, etc.
Images in the set are of 1024x1024 pixel size with 1 meter/pixel resolution. The training is done with Keras toolkit.

#### Metrics
These are the intermediate stage metrics we use to track CNN model improvements and they are pixel based. </br> Pixel precision/recall = 83.06%/80.74%

#### Description
Geometry generation consists of the following steps
1. Image postprocessing
2. Thinning
3. Connectivity improvement
4. Graph construction
5. Finalizing road shapes and network quality
6. Stiching road geojsons between neighboring images where needed

#### Metrics
We use APLS metric to evaluate connectivity. It is measured over images with scale 200x200 meters. </br> APLS precision/recall = 77.61%/71.52%

#### Data Vintage
The vintage of the roads depends on the vintage of the underlying imagery. Because Bing Imagery is a composite of multiple sources it is difficult to know the exact dates for individual pieces of data.

#### How good are the data?
The Osm Missing Data went through a final classifier to ensure that the precision is at least 90%.
Here is another measurement with human OSM editors before the final classifier:
| Label         | %     |
| ------------- |:-------------:|
|Roads added without editing|77%|
|Roads added with minor editing|18%|
|Incorrect roads|5%|

#### Will there be more data coming for other geographies?
Yes, we are working on adding more countries. Next targets are South America and Europe and eventually whole world

#### Why is the data being released?
Microsoft has a continued interest in supporting a thriving OpenStreetMap ecosystem.

#### Should we import the data into OpenStreetMap?
This dataset was shared with Facebook, owner or RapID - a tool for adding mined roads to OSM.

### External References

<table>
    <thead>
        <tr>
			<th colspan=1></th>
            <th colspan=4>Full drop of all mined roads</th>
            <th colspan=4>OSM missing roads</th>
        </tr>
		<tr>
            <th>Date generated</th><th>State</th> <th>Number of Roads</th>  <th>Length km</th> <th>Unzipped MB</th>
			<th>State</th> <th>Number of Roads</th>  <th>Length km</th> <th>Unzipped MB</th>
        </tr>
    </thead>
    <tbody>
		<tr>
			<td>02-May-2020</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/full-roads-set-model25feb2020-geo15oct2019/USA.zip">USA</a></td>
			<td>54484737</td>
			<td>9308940</td><td>13459</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/osm-missing-roads-model25feb2020-geo15oct2019-osm02may2020/USA.zip">USA</a></td>
			<td>5931242</td>
			<td>817761</td>
			<td>2924</td>
		</tr>
		<tr>
			<td>01-March-2021</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/sa5-imgmar2021-modelaug2020-algo15mar2021/Chile.zip">Chile</a></td>
			<td>1052139</td>
			<td>169389</td>
			<td>248</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/sa5-osm18feb2021-imgmar2021-modelaug2020-algo15mar2021/Chile.zip">Chile</a></td>
			<td>22525</td>
			<td>4461</td>
			<td>4.4</td>
		</tr>
		<tr>
			<td>01-March-2021</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/sa5-imgmar2021-modelaug2020-algo15mar2021/Colombia.zip">Colombia</a></td>
			<td>848297</td>
			<td>140948</td>
			<td>191</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/sa5-osm18feb2021-imgmar2021-modelaug2020-algo15mar2021/Colombia.zip">Colombia</a></td>
			<td>10240</td>
			<td>1928</td>
			<td>1.7</td>
		</tr>
		<tr>
			<td>01-March-2021</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/sa5-imgmar2021-modelaug2020-algo15mar2021/Ecuador.zip">Ecuador</a></td>
			<td>562097</td>
			<td>93099</td>
			<td>130</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/sa5-osm18feb2021-imgmar2021-modelaug2020-algo15mar2021/Ecuador.zip">Ecuador</a></td>
			<td>10863</td>
			<td>1752</td>
			<td>1.8</td>
		</tr>
		<tr>
			<td>01-March-2021</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/sa5-imgmar2021-modelaug2020-algo15mar2021/Peru.zip">Peru</a></td>
			<td>727709</td>
			<td>109524</td>
			<td>161</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/sa5-osm18feb2021-imgmar2021-modelaug2020-algo15mar2021/Peru.zip">Peru</a></td>
			<td>5575</td>
			<td>927</td>
			<td>0.9</td>
		</tr>
		<tr>
			<td>01-March-2021</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/sa5-imgmar2021-modelaug2020-algo15mar2021/Venezuela.zip">Venezuela</a></td>
			<td>906586</td>
			<td>162488</td>
			<td>198</td>
			<td><a href="https://usaminedroads.blob.core.windows.net/sa5-osm18feb2021-imgmar2021-modelaug2020-algo15mar2021/Venezuela.zip">Venezuela</a></td>
			<td>20434</td>
			<td>4630</td>
			<td>3.6</td>
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
