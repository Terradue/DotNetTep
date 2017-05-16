using System;
using System.Collections;
using System.Collections.Generic;
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

namespace Terradue.Tep.WebServer.Services {
    
    [Route("/geometry", "POST", Summary = "POST geometry to be converted as WKT")]
    public class GeometryPostRequestTep : IRequiresRequestStream, IReturn<string> {
        public System.IO.Stream RequestStream { get; set; }
    }

    [Route("/geometry/shp", "POST", Summary = "POST geometry to be converted as WKT")]
    public class ShapeFilePostRequestTep : IRequiresRequestStream, IReturn<string> {
        public System.IO.Stream RequestStream { get; set; }
    }

    [Route("/geometry/kml", "POST", Summary = "POST geometry to be converted as WKT")]
    public class KMLPostRequestTep : IRequiresRequestStream, IReturn<string> {
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
            context.Open();
            context.LogInfo(this, string.Format("/geometry/shp GET"));
            string wkt = "blablatest";

            if (this.RequestContext.Files != null && this.RequestContext.Files.Length > 0) { 
                var extension = this.RequestContext.Files[0].FileName.Substring(this.RequestContext.Files[0].FileName.LastIndexOf("."));
                switch (extension) { 
                    case ".zip":
                        using (var stream = new MemoryStream()) {
                            this.RequestContext.Files[0].InputStream.CopyTo(stream);
                            wkt = ExtractWKTFromShapefileZip(stream);
                        }
                    break;
                    case ".kml":
                        using (var stream = new MemoryStream()) {
                            this.RequestContext.Files[0].InputStream.CopyTo(stream);
                            wkt = ExtractWKTFromKML(stream);
                        }   
                    break;
                    default:
                    throw new Exception("Invalid file type");
                    break;
                }
            }

            context.Close();

            if (!string.IsNullOrEmpty(wkt)) return new WebResponseString(wkt);
            else throw new Exception("Unable to get WKT");
        }


        public object Post(ShapeFilePostRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            context.Open();
            context.LogInfo(this, string.Format("/geometry/shp GET"));

            string wkt = null;
            using (var stream = new MemoryStream()) {
                if (this.RequestContext.Files.Length > 0)
                    this.RequestContext.Files[0].InputStream.CopyTo(stream);
                else
                    request.RequestStream.CopyTo(stream);
                wkt = ExtractWKTFromShapefileZip(stream);
            }

            context.Close();
            if (!string.IsNullOrEmpty(wkt)) return new WebResponseString(wkt);
            else throw new Exception("Unable to get WKT");
        }

        public object Post(KMLPostRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            context.Open();
            context.LogInfo(this, string.Format("/geometry/kml GET"));

            string wkt = null;
            using (var stream = new MemoryStream()) {
                if (this.RequestContext.Files.Length > 0)
                    this.RequestContext.Files[0].InputStream.CopyTo(stream);
                else
                    request.RequestStream.CopyTo(stream);
                wkt = ExtractWKTFromKML(stream);
            }

            context.Close();
            if (!string.IsNullOrEmpty(wkt)) return new WebResponseString(wkt);
            else throw new Exception("Unable to get WKT");
        }

        private string ExtractWKTFromShapefileZip(Stream stream) { 
            string uid = Guid.NewGuid().ToString();
            string path = AppDomain.CurrentDomain.BaseDirectory;
            if (!path.EndsWith("/")) path += "/";

            var shapeDir = path + "files/" + uid;
            if (stream.Length > 1 * 1000 * 1000) throw new Exception("We only accept files < 1MB");
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read)) {
                foreach (var entry in archive.Entries) {
                    var extension = entry.FullName.Substring(entry.FullName.LastIndexOf("."));
                    switch (extension) {
                    case ".shp":
                    case ".cpg":
                    case ".shx":
                    case ".idx":
                    case ".dbf":
                    case ".prj":
                    case ".txt":
                    case ".csv":
                        break;
                    default:
                        throw new Exception("Archive contains invalid files");
                    }
                }
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
            if (finalgeometry != null) wkt = SimplifyGeometry(finalgeometry.AsText());
            return wkt;
        }

        private string SimplifyGeometry(string wkt, int pow = 0) {
            if (wkt.Length < 3000)
                return wkt;
            NetTopologySuite.IO.WKTReader wktReader = new NetTopologySuite.IO.WKTReader();
            wktReader.RepairRings = true;
            var geometry = wktReader.Read(wkt);
            if (!geometry.IsValid)
                geometry = geometry.Buffer(0.001);
            var newGeom = NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(geometry, 0.005 * pow);
            NetTopologySuite.IO.WKTWriter wktWriter = new NetTopologySuite.IO.WKTWriter();
            return SimplifyGeometry(wktWriter.Write(newGeom), ++pow);
        }

        private string ExtractWKTFromKML(Stream stream) {
            IGeometry finalgeometry = null;
            stream.Seek(0, SeekOrigin.Begin);

            KmlFile file = KmlFile.Load(stream);
            Kml kml = file.Root as Kml;

            var placemarks = kml.Flatten().OfType<Placemark>();
            foreach (var placemark in placemarks) {
                try {
                    IGeometry geometry = KmlGeometryToGeometry(placemark.Geometry);
                    if (finalgeometry == null) finalgeometry = geometry;
                    else finalgeometry = finalgeometry.Union(geometry);
                } catch (Exception e) {
                    throw new Exception(string.Format("Error with placemark {0}", placemark.Name));
                }
            }
            string wkt = null;
            if (finalgeometry != null) wkt = SimplifyGeometry(finalgeometry.AsText());
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
            }
            return result;
        }
    }

}