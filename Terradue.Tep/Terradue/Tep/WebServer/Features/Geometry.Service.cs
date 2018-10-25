using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ServiceStack.ServiceHost;
using SharpKml.Dom;
using SharpKml.Engine;
using Terradue.Portal;
using Terradue.WebService.Model;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Terradue.Tep.WebServer.Services {

    [Route("/geometry/{points}", "POST", Summary = "POST geometry to be converted as WKT")]
    public class GeometryPostRequestTep : IRequiresRequestStream, IReturn<string> {
        public System.IO.Stream RequestStream { get; set; }

        [ApiMember(Name = "points", Description = "Points", ParameterType = "path", DataType = "int", IsRequired = true)]
        public int points { get; set; }
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
                                wkt = ExtractWKTFromGeoJson(stream);
                            }
                            break;
                        default:
                            throw new Exception("Invalid file type");
                            break;
                    }
                }

                context.Close();
            }catch(Exception e){
                context.LogError(this,e.Message + "-" + e.StackTrace);
                context.Close();
                throw e;
            }

            if (!string.IsNullOrEmpty(wkt)) return new WebResponseString(wkt);
            else throw new Exception("Unable to get WKT");
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
            IGeometry finalgeometry = null;

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
                    p.Z = Math.Round(p.Z, 2);
                }
                wkt = finalgeometry.AsText();
            }
            return wkt;
        }

        private IGeometry SimplifyGeometry(IGeometry geometry, int nbPoints, int pow = 1) {
            if (geometry.NumPoints < nbPoints)
                return geometry;
            if (!geometry.IsValid) geometry = geometry.Buffer(0.001);
            var newGeom = NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(geometry, 0.005 * pow);
            return SimplifyGeometry(newGeom, nbPoints, ++pow);
        }

        private string ExtractWKTFromKML(Stream stream, int points) {
            IGeometry finalgeometry = null;
            try {
                stream.Seek(0, SeekOrigin.Begin);
            }catch(Exception){}
            KmlFile file = KmlFile.Load(stream);
            Kml kml = file.Root as Kml;

            var placemarks = kml.Flatten().OfType<Placemark>();
            foreach (var placemark in placemarks) {
                try {
                    IGeometry geometry = KmlGeometryToGeometry(placemark.Geometry);
                    if (finalgeometry == null) finalgeometry = geometry;
                    else finalgeometry = finalgeometry.Union(geometry);
                } catch (Exception e) {
                    //throw new Exception(string.Format("Error with placemark {0}", placemark.Name));
                }
            }
            string wkt = null;
            if (finalgeometry != null) {
                finalgeometry = SimplifyGeometry(finalgeometry, points);
                foreach (var p in finalgeometry.Coordinates.ToArray()) {
                    p.X = Math.Round(p.X, 2);
                    p.Y = Math.Round(p.Y, 2);
                    p.Z = Math.Round(p.Z, 2);
                }
                wkt = finalgeometry.AsText();
            }
            return wkt;
        }

        private IGeometry KmlGeometryToGeometry(SharpKml.Dom.Geometry geometry) {
            IGeometry result = null;
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
            if (fc.Features != null && fc.Features.Count > 0)
                return Terradue.GeoJson.Geometry.WktExtensions.ToWkt(fc.Features[0]);
            else throw new Exception("no feature found in the geojson");
        }
    }

}