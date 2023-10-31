using System;
using System.IO;
using Aspose.Svg;
using Aspose.Svg.Rendering.Image;
using SharedProject2;
using System.IO.Compression;
using System.Collections.Generic;
using MongoDB.Driver;
using System.Reflection;
using System.Linq;
using System.Globalization;
using System.Xml.Linq;

namespace Portal.Misc
{
    public class ImageBuilder
    {
        private GptwLog AtlasLog;
        private GptwLogContext gptwContext;
        private AtlasOperationsDataRepository repo = null;

        public ImageBuilder(AtlasOperationsDataRepository repo, GptwLogContext gptwContext, GptwLog AtlasLog)
        {
            this.repo = repo;
            this.gptwContext = gptwContext;
            this.AtlasLog = AtlasLog;
        }

        public Stream CreateImageStream(string imageType, string imageTemplate, Dictionary<string, KeyValuePair<string, string>> dynamicContent)
        {

            try
            {

                using (Stream templateStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(imageTemplate))
                {

                    // Load the xdocument with the template stream
                    XDocument xdocument = XDocument.Load(templateStream);

                    // Need the namespace for searching for elements by name
                    XNamespace ns = xdocument.Root.Name.Namespace;

                    // Gather the dynamic content    
                    ImageBuilder imageBuilder = new ImageBuilder(repo, gptwContext, AtlasLog);

                    // Modify the template with the dynamic content
                    string elementId = "";
                    string content = "";
                    string elementType = "";

                    foreach (KeyValuePair<string, KeyValuePair<string, string>> dynamicItem in dynamicContent)
                    {
                        elementId = dynamicItem.Key;
                        content = dynamicItem.Value.Key;
                        elementType = dynamicItem.Value.Value;

                        switch (elementType.ToLower())
                        {
                            case "foreignobject":
                                // Get the paragraph element with the specified attribute id value
                                // Note the dynamic content paragraph tags have their own namespace
                                XElement pElement = xdocument.Descendants("{http://www.w3.org/1999/xhtml}p").FirstOrDefault(el => el.Attribute("id")?.Value.ToLower() == elementId.ToLower());

                                if (pElement != null)
                                {
                                    // Dynamically replace the text content
                                    // Insert it as CDATA so any embedded characters are not interpreted as markup
                                    // TODO: what about a case where the content could contain intentional markup?
                                    pElement.ReplaceNodes(new XCData(content));
                                }
                                break;
                            case "text":
                                // Get the text element with the specified attribute id value
                                XElement textElement = xdocument.Descendants(ns + "text").FirstOrDefault(el => el.Attribute("id")?.Value.ToLower() == elementId.ToLower());

                                if (textElement != null)
                                {
                                    // Dynamically replace the text content
                                    // Insert it as CDATA so any embedded characters are not interpreted as markup
                                    textElement.ReplaceNodes(new XCData(content));
                                }
                                break;
                            case "bar":
                                // Get the rect element with the specified attribute id value
                                XElement rectElement = xdocument.Descendants(ns + "rect").FirstOrDefault(el => el.Attribute("id")?.Value.ToLower() == elementId.ToLower());

                                if (rectElement != null)
                                {
                                    // Dynamically replace the width value
                                    rectElement.Attribute("width").Value = content;
                                }
                                break;
                            case "line":
                                // Get the line element with the specified attribute id value
                                XElement lineElement = xdocument.Descendants(ns + "line").FirstOrDefault(el => el.Attribute("id")?.Value.ToLower() == elementId.ToLower());

                                if (lineElement != null)
                                {
                                    // Dynamically replace the x1 and x2 values
                                    lineElement.Attribute("x1").Value = content;
                                    lineElement.Attribute("x2").Value = content;
                                }
                                break;
                        }
                    }

                    Stream svgStream = new MemoryStream();
                    xdocument.Save(svgStream);

                    Stream outputStream = new MemoryStream();
                    switch (imageType.ToLower())
                    {
                        case "png":
                            Stream pngStream = ConvertSvgToPng(svgStream);
                            pngStream.Position = 0;
                            return pngStream;
                        case "jpg":
                            Stream jpgStream = ConvertSvgToJpg(svgStream);
                            jpgStream.Position = 0;
                            return jpgStream;
                        default:
                            // no conversion, return the svg stream;
                            svgStream.Position = 0;
                            return svgStream;
                    }
                }
            }

            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unable to render an image"), e, gptwContext);
                return null;
            }
        }

        public Stream ConvertSvgToPng(Stream svgStream)
        {
            setAsposeLicense();

            svgStream.Position = 0;
            using (SVGDocument document = new SVGDocument(svgStream, ""))
            {
                ImageRenderingOptions imageOptions = new ImageRenderingOptions(ImageFormat.Png);
                // Apply anti-aliasing
                imageOptions.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                Stream pngStream = new MemoryStream();
                using (ImageDevice device = new ImageDevice(imageOptions, pngStream))
                {
                    document.RenderTo(device);
                }
                pngStream.Position = 0;
                return pngStream;
            }
        }

        public Stream ConvertSvgToJpg(Stream svgStream)
        {
            setAsposeLicense();

            svgStream.Position = 0;
            using (SVGDocument document = new SVGDocument(svgStream, ""))
            {
                ImageRenderingOptions imageOptions = new ImageRenderingOptions(ImageFormat.Jpeg);
                // Apply anti-aliasing
                imageOptions.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                Stream jpgStream = new MemoryStream();
                using (ImageDevice device = new ImageDevice(imageOptions, jpgStream))
                {
                    document.RenderTo(device);
                }
                jpgStream.Position = 0;
                return jpgStream;
            }
        }

        /// <summary>
        /// Create a streamed Zip archive of dynamic badges in svg, png, and jpg formats
        /// </summary>
        /// <param name="engagementId"></param>
        /// <returns>Completed Zip archive as a memory stream</returns>
        public Stream createBadgeZipStream(int engagementId)
        {
            BadgeContentHelpers badgeContentHelpers = new BadgeContentHelpers(repo, gptwContext, AtlasLog);
            BadgeContentHelpers.BadgeInfo badgeInfo = badgeContentHelpers.getBadgeInfo(engagementId, gptwContext);
            Dictionary<string, KeyValuePair<string, string>> dynamicContent = badgeContentHelpers.gatherBadgeDynamicContent(engagementId, gptwContext);
            string badgeTemplate = badgeInfo.BadgeTemplate;
            string badgeFilename = badgeContentHelpers.buildBadgeFilename(engagementId, gptwContext);
            Stream badgeStream = createBadgeZipStream(badgeTemplate, dynamicContent, badgeFilename);
            return badgeStream;
        }

        /// <summary>
        /// Create a streamed Zip archive of dynamic badges in svg, png, and jpg formats
        /// </summary>
        /// <param name="badgeTemplate">Badge template file ready to be filled in with certification-specific dynamic content</param>
        /// <param name="dynamicContent">Dynamic content to apply to the badge template</param>
        /// <param name="badgeFilename">Filename to give the individual files in the archive, not including the extension</param>
        /// <returns>Completed Zip archive as a memory stream</returns>
        public Stream createBadgeZipStream(string badgeTemplate, Dictionary<string, KeyValuePair<string, string>> dynamicContent, string badgeFilename)
        {

            try
            {
                // Create Zip archive
                Stream archiveStream = new MemoryStream();
                using (ZipArchive archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
                {

                    // Create an entry for the svg badge
                    ZipArchiveEntry badgeSvg = archive.CreateEntry(badgeFilename + ".svg");
                    using (Stream entryStream = badgeSvg.Open())
                    {
                        Stream svgBadgeStream = CreateImageStream("svg", badgeTemplate, dynamicContent);
                        using (Stream fileToCompressStream = svgBadgeStream)
                        {
                            fileToCompressStream.CopyTo(entryStream);
                        }
                    }

                    // Create an entry for the png badge
                    ZipArchiveEntry badgePng = archive.CreateEntry(badgeFilename + ".png");
                    using (Stream entryStream = badgePng.Open())
                    {
                        Stream pngBadgeStream = CreateImageStream("png", badgeTemplate, dynamicContent);
                        using (Stream fileToCompressStream = pngBadgeStream)
                        {
                            fileToCompressStream.CopyTo(entryStream);
                        }
                    }

                    // Create an entry for the jpg badge
                    ZipArchiveEntry badgeJpg = archive.CreateEntry(badgeFilename + ".jpg");
                    using (Stream entryStream = badgeJpg.Open())
                    {
                        Stream jpgBadgeStream = CreateImageStream("jpg", badgeTemplate, dynamicContent);
                        using (Stream fileToCompressStream = jpgBadgeStream)
                        {
                            fileToCompressStream.CopyTo(entryStream);
                        }
                    }

                }

                archiveStream.Position = 0;
                return archiveStream;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unable to render a badge"), e, gptwContext);
                return null;
            }
        }

        private void setAsposeLicense()
        {
            try
            {
                Aspose.Svg.License AsposeLic = new Aspose.Svg.License();
                AsposeLic.SetLicense("Aspose.Total.lic");
            }
            catch (Exception e)
            {
                string errorMessage = "Error setting Aspose license";
                AtlasLog.LogErrorWithException(errorMessage, e, gptwContext);
                throw (new Exception(errorMessage));
            }
        }
    }
}

