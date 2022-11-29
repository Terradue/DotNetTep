﻿using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ServiceStack.ServiceHost;
using SharpKml.Dom;
using SharpKml.Engine;
using Terradue.Portal;
using Terradue.WebService.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using Terradue.Stars.Geometry.Wkt;

namespace Terradue.Tep.WebServer.Services {

    [Route("/geometry/{points}", "POST", Summary = "POST geometry to be converted as WKT")]
    public class GeometryPostRequestTep : IRequiresRequestStream, IReturn<string> {
        public System.IO.Stream RequestStream { get; set; }

        [ApiMember(Name = "points", Description = "Points", ParameterType = "path", DataType = "int", IsRequired = true)]
        public int points { get; set; }

        [ApiMember(Name = "format", Description = "format", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string format { get; set; }
    }

    [Route("/geometry/shp/{Points}", "POST", Summary = "POST geometry to be converted as WKT")]
    public class ShapeFilePostRequestTep : IRequiresRequestStream, IReturn<string> {
        public System.IO.Stream RequestStream { get; set; }

        [ApiMember(Name = "points", Description = "Points", ParameterType = "path", DataType = "int", IsRequired = true)]
        public int points { get; set; }
    }

    [Route("/geometry/kml/{Points}", "POST", Summary = "POST geometry to be converted as WKT")]
    public class KMLPostRequestTep : IRequiresRequestStream, IReturn<string> {
        public System.IO.Stream RequestStream { get; set; }

        [ApiMember(Name = "points", Description = "Points", ParameterType = "path", DataType = "int", IsRequired = true)]
        public int points { get; set; }
    }

	[Route("/geometry/geojson", "POST", Summary = "POST geometry to be converted as WKT")]
	public class GeoJsonPostRequestTep : IRequiresRequestStream, IReturn<string> {
        public System.IO.Stream RequestStream { get; set; }
	}

    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
             EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class GeometryServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public object Post(GeometryPostRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            string wkt = "";

            try {
                context.Open();
                context.LogInfo(this, string.Format("/geometry/{0} GET", request.points));

                if (request.points == 0) {
                    var segments = base.Request.PathInfo.Split(new[] { '/' },
                                                               StringSplitOptions.RemoveEmptyEntries);
                    request.points = System.Int32.Parse(segments[segments.Count() - 1]);
                }
                if(string.IsNullOrEmpty(request.format) && base.Request.QueryString != null && base.Request.QueryString["format"] != null){
                    request.format = base.Request.QueryString["format"];
                }

                var points = request.points > 0 ? request.points : 3000;

                if (this.RequestContext.Files != null && this.RequestContext.Files.Length > 0) {
                    var extension = this.RequestContext.Files[0].FileName.Substring(this.RequestContext.Files[0].FileName.LastIndexOf("."));
                    switch (extension) {
                        case ".zip":
                            using (var stream = new MemoryStream()) {
                                this.RequestContext.Files[0].InputStream.CopyTo(stream);
                                wkt = ExtractWKTFromShapefileZip(stream, points);
                            }
                            break;
                        case ".kml":
                            using (var stream = new MemoryStream()) {
                                this.RequestContext.Files[0].InputStream.CopyTo(stream);
                                wkt = ExtractWKTFromKML(stream, points);
                            }
                            break;
                        case ".kmz":
                            using (var stream = new MemoryStream()) {
                                this.RequestContext.Files[0].InputStream.CopyTo(stream);
                                ZipArchive archive = new ZipArchive(stream);
                                foreach (ZipArchiveEntry entry in archive.Entries) {
                                    if (entry.FullName.EndsWith(".kml", StringComparison.OrdinalIgnoreCase)) {
                                        using (var unzippedEntryStream = entry.Open()) {
                                            wkt = ExtractWKTFromKML(unzippedEntryStream, points);
                                            break;
                                        }
                                    }
                                }
                            }
                            break;
                        case ".json":
                        case ".geojson":
                            using (var stream = new MemoryStream()) {
                                this.RequestContext.Files[0].InputStream.CopyTo(stream);
                                wkt = ExtractWKTFromGeoJson(stream, points);
                            }
                            break;
                        default:
                            throw new Exception("Invalid file type");
                    }
                }

                context.Close();
            }catch(Exception e){
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }

            if (!string.IsNullOrEmpty(wkt)){
                if(request.format == "geojson") 
                    return new WebResponseString(WktToGeoJson(wkt));
                else 
                    return new WebResponseString(wkt);
            }else throw new Exception("Unable to get WKT");
        }


        public object Post(ShapeFilePostRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            context.Open();
            context.LogInfo(this, string.Format("/geometry/shp GET"));

            if (request.points == 0) {
                var segments = base.Request.PathInfo.Split(new[] { '/' },
                                                           StringSplitOptions.RemoveEmptyEntries);
                request.points = System.Int32.Parse(segments[segments.Count() - 1]);
            }

            var points = request.points > 0 ? request.points : 3000;

            string wkt = null;
            using (var stream = new MemoryStream()) {
                if (this.RequestContext.Files.Length > 0)
                    this.RequestContext.Files[0].InputStream.CopyTo(stream);
                else
                    request.RequestStream.CopyTo(stream);
                wkt = ExtractWKTFromShapefileZip(stream, points);
            }

            context.Close();
            if (!string.IsNullOrEmpty(wkt)) return new WebResponseString(wkt);
            else throw new Exception("Unable to get WKT");
        }

        public object Post(KMLPostRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            context.Open();
            context.LogInfo(this, string.Format("/geometry/kml GET"));

            if (request.points == 0) {
                var segments = base.Request.PathInfo.Split(new[] { '/' },
                                                           StringSplitOptions.RemoveEmptyEntries);
                request.points = System.Int32.Parse(segments[segments.Count() - 1]);
            }

            var points = request.points > 0 ? request.points : 3000;

            string wkt = null;
            using (var stream = new MemoryStream()) {
                if (this.RequestContext.Files.Length > 0)
                    this.RequestContext.Files[0].InputStream.CopyTo(stream);
                else
                    request.RequestStream.CopyTo(stream);
                wkt = ExtractWKTFromKML(stream, points);
            }

            context.Close();
            if (!string.IsNullOrEmpty(wkt)) return new WebResponseString(wkt);
            else throw new Exception("Unable to get WKT");
        }

		public object Post(GeoJsonPostRequestTep request) {
			var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
			context.Open();
			context.LogInfo(this, string.Format("/geometry/geojson GET"));

			string wkt = null;
			using (var stream = new MemoryStream()) {
				if (this.RequestContext.Files.Length > 0)
					this.RequestContext.Files[0].InputStream.CopyTo(stream);
				else
					request.RequestStream.CopyTo(stream);
				wkt = ExtractWKTFromGeoJson(stream);
			}

			context.Close();
			if (!string.IsNullOrEmpty(wkt)) return new WebResponseString(wkt);
			else throw new Exception("Unable to get WKT");
		}

        private string ExtractWKTFromShapefileZip(Stream stream, int points) { 
            string uid = Guid.NewGuid().ToString();
            string path = AppDomain.CurrentDomain.BaseDirectory;
            if (!path.EndsWith("/")) path += "/";

            var shapeDir = path + "files/" + uid;
            if (stream.Length > 1 * 1000 * 1000) throw new Exception("We only accept files < 1MB");
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read)) {
                archive.ExtractToDirectory(shapeDir);
            }

            //get all .shp files in unziped directory
            var myFiles = Directory.GetFiles(shapeDir, "*.shp", SearchOption.AllDirectories);
            GeometryFactory factory = new GeometryFactory();
            NetTopologySuite.Geometries.Geometry finalgeometry = null;

            foreach (var shapefile in myFiles) {
                ShapefileDataReader shapeFileDataReader = new ShapefileDataReader(shapefile, factory);
                while (shapeFileDataReader.Read()) {
                    var geometry = (NetTopologySuite.Geometries.Geometry)shapeFileDataReader.Geometry;
                    if (finalgeometry == null) finalgeometry = geometry;
                    else finalgeometry = finalgeometry.Union(geometry);
                }
                //Close and free up any resources
                shapeFileDataReader.Close();
                shapeFileDataReader.Dispose();
            }

            string wkt = null;
            if (finalgeometry != null) {
                finalgeometry = SimplifyGeometry(finalgeometry, points);
                foreach (var p in finalgeometry.Coordinates.ToArray()) {
                    p.X = Math.Round(p.X, 2);
                    p.Y = Math.Round(p.Y, 2);
                    if (p is CoordinateZ) p.Z = Math.Round(p.Z, 2);
                }
                wkt = finalgeometry.AsText();
            }
            return wkt;
        }

        private NetTopologySuite.Geometries.Geometry SimplifyGeometry(NetTopologySuite.Geometries.Geometry geometry, int nbPoints, int pow = 1) {
            if (geometry.NumPoints < nbPoints)
                return geometry;
            if (!geometry.IsValid) geometry = geometry.Buffer(0.001);
            var newGeom = NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(geometry, 0.005 * pow);
            return SimplifyGeometry(newGeom, nbPoints, ++pow);
        }

        private string ExtractWKTFromKML(Stream stream, int points) {
            NetTopologySuite.Geometries.Geometry finalgeometry = null;
            try {
                stream.Seek(0, SeekOrigin.Begin);
            }catch(Exception){}
            KmlFile file = KmlFile.Load(stream);
            Kml kml = file.Root as Kml;

            var placemarks = kml.Flatten().OfType<Placemark>();
            foreach (var placemark in placemarks) {
                try {
                    NetTopologySuite.Geometries.Geometry geometry = KmlGeometryToGeometry(placemark.Geometry);
                    if (finalgeometry == null) finalgeometry = geometry;
                    else finalgeometry = finalgeometry.Union(geometry);
                } catch (Exception) {
                    //throw new Exception(string.Format("Error with placemark {0}", placemark.Name));
                }
            }
            string wkt = null;
            if (finalgeometry != null) {
                finalgeometry = SimplifyGeometry(finalgeometry, points);
                foreach (var p in finalgeometry.Coordinates.ToArray()) {
                    p.X = Math.Round(p.X, 2);
                    p.Y = Math.Round(p.Y, 2);
                    if (p is CoordinateZ) p.Z = Math.Round(p.Z, 2);
                }
                wkt = finalgeometry.AsText();
            }
            return wkt;
        }

        private NetTopologySuite.Geometries.Geometry KmlGeometryToGeometry(SharpKml.Dom.Geometry geometry) {
            NetTopologySuite.Geometries.Geometry result = null;
            if (geometry is SharpKml.Dom.Point) {
                var kmlPoint = geometry as SharpKml.Dom.Point;
                result = new NetTopologySuite.Geometries.Point(new Coordinate(kmlPoint.Coordinate.Longitude, kmlPoint.Coordinate.Latitude));
            } else if (geometry is SharpKml.Dom.Polygon) {                
                var kmlPolygon = geometry as SharpKml.Dom.Polygon;
                if (kmlPolygon.OuterBoundary == null || kmlPolygon.OuterBoundary.LinearRing == null || kmlPolygon.OuterBoundary.LinearRing.Coordinates == null) throw new Exception("Polygon is null");
                var coordinates = new Coordinate[kmlPolygon.OuterBoundary.LinearRing.Coordinates.Count];
                int i = 0;
                foreach (var coordinate in kmlPolygon.OuterBoundary.LinearRing.Coordinates) {
                    coordinates[i++] = new Coordinate(coordinate.Longitude, coordinate.Latitude);
                }
                result = new NetTopologySuite.Geometries.Polygon(new NetTopologySuite.Geometries.LinearRing(coordinates));
            } else if (geometry is SharpKml.Dom.MultipleGeometry){
                var mgeometry = geometry as SharpKml.Dom.MultipleGeometry;
                var polygons = new List<NetTopologySuite.Geometries.Polygon>();
                foreach(var poly in mgeometry.Geometry){
                    var kmlPolygon = poly as SharpKml.Dom.Polygon;
                    if (kmlPolygon.OuterBoundary == null || kmlPolygon.OuterBoundary.LinearRing == null || kmlPolygon.OuterBoundary.LinearRing.Coordinates == null) throw new Exception("Polygon is null");
                    var coordinates = new Coordinate[kmlPolygon.OuterBoundary.LinearRing.Coordinates.Count];
                    int i = 0;
                    foreach (var coordinate in kmlPolygon.OuterBoundary.LinearRing.Coordinates) {
                        coordinates[i++] = new Coordinate(coordinate.Longitude, coordinate.Latitude);
                    }
                    polygons.Add(new NetTopologySuite.Geometries.Polygon(new NetTopologySuite.Geometries.LinearRing(coordinates)));
                }
                result = new NetTopologySuite.Geometries.MultiPolygon(polygons.ToArray());
            }
            return result;
        }
        
        private string ExtractWKTFromGeoJson(Stream stream) {
            stream.Seek(0, SeekOrigin.Begin);

			Terradue.GeoJson.Feature.FeatureCollection fc;
			var serializer = new JsonSerializer();

            using (var sr = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(sr)) {
                fc = serializer.Deserialize<Terradue.GeoJson.Feature.FeatureCollection>(jsonTextReader);
            }
            if (fc.Features != null && fc.Features.Count > 0) {

                if (fc.Features.Count == 1)
                    return Terradue.GeoJson.Geometry.WktExtensions.ToWkt(fc.Features[0]);

                var polygons = new Terradue.GeoJson.Geometry.MultiPolygon();
                polygons.Polygons = new List<GeoJson.Geometry.Polygon>();
                foreach (var feature in fc.Features) {
                    var geom = feature.Geometry;
                    if (geom is Terradue.GeoJson.Geometry.MultiPolygon) {
                        foreach(var poly in ((GeoJson.Geometry.MultiPolygon)geom).Polygons){
                            polygons.Polygons.Add(poly);
                        }
                    }
                    else if (geom is Terradue.GeoJson.Geometry.Polygon) {
                        polygons.Polygons.Add((GeoJson.Geometry.Polygon)geom);
                    }
                }
                return Terradue.GeoJson.Geometry.WktExtensions.ToWkt(polygons);
            } else throw new Exception("no feature found in the geojson");
        }

        private string ExtractWKTFromGeoJson(Stream stream, int points) {
            var wkt = ExtractWKTFromGeoJson(stream);
            NetTopologySuite.IO.WKTReader wktReader = new NetTopologySuite.IO.WKTReader();
            // wktReader.RepairRings = true;
            var finalgeometry = wktReader.Read(wkt);

            if (finalgeometry != null) {
                finalgeometry = SimplifyGeometry(finalgeometry, points);
                foreach (var p in finalgeometry.Coordinates.ToArray()) {
                    p.X = Math.Round(p.X, 2);
                    p.Y = Math.Round(p.Y, 2);
                    if (p is CoordinateZ) p.Z = Math.Round(p.Z, 2);
                }
                wkt = finalgeometry.AsText();
            }
            return wkt;
        }

        private string WktToGeoJson(string wkt){
            var geometry = WktExtensions.WktToGeometry(wkt);
            return GetJson(geometry);
        }

        private string GetJson(object geometry){
            var serializer = new JsonSerializer {NullValueHandling = NullValueHandling.Ignore};
            string jsonouts;

            using (StringWriter sw = new StringWriter())
            using (JsonTextWriter jw = new JsonTextWriter(sw)) {
                serializer.Serialize(jw, geometry);
                jsonouts = sw.ToString();
            }
            return jsonouts;
        }
    }

}