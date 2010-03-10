﻿using System;
using System.IO;
using System.Net;
using System.Xml.Schema;
using Sage.SData.Client.Atom;
using Sage.SData.Client.Common;
using Sage.SData.Client.Extensions;
using Sage.SData.Client.Framework;

namespace Sage.SData.Client.Core
{
    /// <summary>
    /// Service class for processing SData Request
    /// </summary>
    /// <example>
    ///     <code lang="cs" title="The following code example demonstrates the usage of the SDataService class.">
    ///         <code 
    ///             source=".\Example.cs" 
    ///             region="SDataService Configuration" 
    ///         />
    ///     </code>
    /// </example>
    public class SDataService : ISDataService, ISDataRequestSettings
    {
        private readonly SDataUri _uri;

        /// <summary>
        /// Flag set when Service is initialzed
        /// </summary>
        [Obsolete("Explicit initialization is no longer required.")]
        public bool Initialized
        {
            get { return true; }
        }

        /// <remarks>
        /// Creates the service with predefined values for the url
        /// </remarks>
        public string Url
        {
            get { return _uri.ToString(); }
            set { _uri.Uri = new Uri(value); }
        }

        /// <summary>
        /// Accessor method for protocol, 
        /// </summary>
        /// <remarks>HTTP is the default but can be HTTPS</remarks>
        public string Protocol
        {
            get { return _uri.Scheme; }
            set { _uri.Scheme = value; }
        }

        /// <remarks>
        /// IP address is also allowed (192.168.1.1).
        /// Can be followed by port number. For example www.example.com:5493. 
        /// 5493 is the recommended port number for SData services that are not exposed on the Internet.
        /// </remarks>
        public string ServerName
        {
            get { return _uri.Host; }
            set
            {
                if (_uri.Host != value)
                {
                    var pos = value.IndexOf(':');
                    int port;

                    if (pos >= 0 && int.TryParse(value.Substring(pos + 1), out port))
                    {
                        _uri.Host = value.Substring(0, pos);
                        _uri.Port = port;
                    }
                    else
                    {
                        _uri.Host = value;
                    }
                }
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public int? Port
        {
            get { return _uri.Port; }
            set { _uri.Port = value ?? 80; }
        }

        /// <summary>
        /// Accessor method for virtual directory
        /// </summary>
        /// <remarks>Must be sdata, unless the technical framework imposes something different.
        ///</remarks>
        public string VirtualDirectory
        {
            get { return _uri.Server; }
            set { _uri.Server = value; }
        }

        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        /// <value>The name of the application.</value>
        /// <remarks>
        ///     The <see cref="ApplicationName"/> is used to identify users specific to an application. That is, the same syndication resource can exist in the data store 
        ///     for multiple applications that specify a different <see cref="ApplicationName"/>. This enables multiple applications to use the same data store to store resource 
        ///     information without running into duplicate syndication resource conflicts. Alternatively, multiple applications can use the same syndication resource data store 
        ///     by specifying the same <see cref="ApplicationName"/>. The <see cref="ApplicationName"/> can be set programmatically or declaratively in the configuration for the application.
        /// </remarks>
        public string ApplicationName
        {
            get { return _uri.Product; }
            set { _uri.Product = value; }
        }

        /// <summary>
        /// Accessor method for contractName
        /// </summary>
        /// <remarks>An SData service can support several “integration contracts” side-by-side. 
        /// For example, a typical Sage ERP service will support a crmErp contract which exposes 
        /// the resources required by CRM integration (with schemas imposed by the CRM/ERP contract) 
        /// and a native or default contract which exposes all the resources of the ERP in their native format.
        /// </remarks>
        public string ContractName
        {
            get { return _uri.Contract; }
            set { _uri.Contract = value; }
        }

        /// <summary>
        /// Accessor method for dataSet
        /// </summary>
        /// <remarks>Identifies the dataset when the application gives access to several datasets, such as several companies and production/test datasets.
        /// If the application can only handle a single dataset, or if it can be configured with a default dataset, 
        /// a hyphen can be used as a placeholder for the default dataset. 
        /// For example, if prod is the default dataset in the example above, the URL could be shortened as:
        /// http://www.example.com/sdata/sageApp/test/-/accounts?startIndex=21&amp;count=10 
        /// If several parameters are required to specify the dataset (for example database name and company id), 
        /// they should be formatted as a single segment in the URL. For example, sageApp/test/demodb;acme/accounts -- the semicolon separator is application specific, not imposed by SData.
        ///</remarks>
        public string DataSet
        {
            get { return _uri.CompanyDataset; }
            set { _uri.CompanyDataset = value; }
        }

        /// <summary>
        /// Get set for the user name to authenticate with
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Get/set for the password to authenticate with
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// TODO
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Gets the cookie collection associated with all requests to the server.
        /// </summary>
        public CookieContainer Cookies { get; set; }

        /// <summary>
        /// TODO
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Adds a new syndication resource to the data source.
        /// </summary>
        /// <param name="request">The request that identifies the resource within the syndication data source.</param>
        /// <param name="feed"></param>
        public AtomFeed CreateFeed(SDataBaseRequest request, AtomFeed feed)
        {
            string eTag;
            return CreateFeed(request, feed, out eTag);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="request"></param>
        /// <param name="feed"></param>
        /// <param name="eTag"></param>
        /// <returns></returns>
        public AtomFeed CreateFeed(SDataBaseRequest request, AtomFeed feed, out string eTag)
        {
            Guard.ArgumentNotNull(request, "request");
            Guard.ArgumentNotNull(feed, "feed");

            try
            {
                var requestUrl = request.ToString();
                var operation = new RequestOperation(HttpMethod.Post, feed);
                var response = ExecuteRequest(requestUrl, operation, MediaType.Atom);
                eTag = response.ETag;
                return (AtomFeed) response.Content;
            }
            catch (WebException ex)
            {
                throw new SDataServiceException(ex);
            }
            catch (Exception ex)
            {
                throw new SDataClientException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Adds a new syndication resource to the data source.
        /// </summary>
        /// <param name="request">The request that identifies the resource within the syndication data source.</param>
        /// <param name="entry">TODO</param>
        public AtomEntry CreateEntry(SDataBaseRequest request, AtomEntry entry)
        {
            Guard.ArgumentNotNull(request, "request");
            Guard.ArgumentNotNull(entry, "entry");

            try
            {
                var requestUrl = request.ToString();

                if (BatchProcess.Instance.Requests.Count > 0)
                {
                    var batchRequest = new SDataBatchRequestItem
                                       {
                                           Url = requestUrl,
                                           Method = HttpMethod.Post,
                                           Entry = entry
                                       };
                    BatchProcess.Instance.AddToBatch(batchRequest);
                    return null;
                }

                var operation = new RequestOperation(HttpMethod.Post, entry);
                var response = ExecuteRequest(requestUrl, operation, MediaType.AtomEntry);
                entry = (AtomEntry) response.Content;

                if (!string.IsNullOrEmpty(response.ETag))
                {
                    entry.SetSDataHttpETag(response.ETag);
                }

                return entry;
            }
            catch (WebException ex)
            {
                throw new SDataServiceException(ex);
            }
            catch (Exception ex)
            {
                throw new SDataClientException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Asynchronous PUT to the server
        /// </summary>
        /// <param name="request">The request that identifies the resource within the syndication data source.</param>
        /// <param name="resource">TODO</param>
        public AsyncRequest CreateAsync(SDataBaseRequest request, ISyndicationResource resource)
        {
            Guard.ArgumentNotNull(request, "request");
            Guard.ArgumentNotNull(resource, "resource");

            try
            {
                var requestUrl = new SDataUri(request.ToString()) {TrackingId = Guid.NewGuid().ToString()}.ToString();
                var operation = new RequestOperation(HttpMethod.Post, resource);
                var response = ExecuteRequest(requestUrl, operation, MediaType.Xml);
                var tracking = (SDataTracking) response.Content;
                return new AsyncRequest(this, response.Location, tracking);
            }
            catch (WebException ex)
            {
                throw new SDataServiceException(ex);
            }
            catch (Exception ex)
            {
                throw new SDataClientException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Generic delete from server
        /// </summary>
        /// <param name="url">the url for the operation</param>
        /// <returns><b>true</b> returns true if the operation was successful</returns>
        public bool Delete(string url)
        {
            Guard.ArgumentNotNull(url, "url");

            try
            {
                var operation = new RequestOperation(HttpMethod.Delete);
                var response = ExecuteRequest(url, operation, null);
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch (WebException ex)
            {
                throw new SDataServiceException(ex);
            }
            catch (Exception ex)
            {
                throw new SDataClientException(ex.Message, ex);
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public bool DeleteEntry(SDataBaseRequest request)
        {
            return DeleteEntry(request, null);
        }

        /// <summary>
        /// Removes a resource from the syndication data source.
        /// </summary>
        /// <param name="request">The request from the syndication data source for the resource to be removed.</param>
        /// <param name="entry">the resource that is being deleted</param>
        /// <returns><b>true</b> if the syndication resource was successfully deleted; otherwise, <b>false</b>.</returns>
        public bool DeleteEntry(SDataBaseRequest request, AtomEntry entry)
        {
            Guard.ArgumentNotNull(request, "request");

            try
            {
                var requestUrl = request.ToString();
                var eTag = entry != null ? entry.GetSDataHttpETag() : null;

                if (BatchProcess.Instance.Requests.Count > 0)
                {
                    var batchRequest = new SDataBatchRequestItem
                                       {
                                           Url = requestUrl,
                                           Method = HttpMethod.Delete,
                                           ETag = eTag
                                       };
                    BatchProcess.Instance.AddToBatch(batchRequest);
                    return true;
                }

                var operation = new RequestOperation(HttpMethod.Delete) {ETag = eTag};
                var response = ExecuteRequest(requestUrl, operation, null);
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch (WebException ex)
            {
                throw new SDataServiceException(ex);
            }
            catch (Exception ex)
            {
                throw new SDataClientException(ex.Message, ex);
            }
        }

        /// <summary>
        /// generic read from the specified url
        /// </summary>
        /// <param name="url">url to read from </param>
        /// <returns>string response from server</returns>
        public object Read(string url)
        {
            Guard.ArgumentNotNull(url, "url");

            try
            {
                var operation = new RequestOperation(HttpMethod.Get);
                var response = ExecuteRequest(url, operation, null);
                return response.Content;
            }
            catch (WebException ex)
            {
                throw new SDataServiceException(ex);
            }
            catch (Exception ex)
            {
                throw new SDataClientException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Reads resource information from the data source based on the URL.
        /// </summary>
        /// <param name="request">request for the syndication resource to get information for.</param>
        /// <returns>AtomFeed <see cref="AtomFeed"/> populated with the specified resources's information from the data source.</returns>
        public AtomFeed ReadFeed(SDataBaseRequest request)
        {
            string eTag = null;
            return ReadFeed(request, ref eTag);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="request"></param>
        /// <param name="eTag"></param>
        /// <returns></returns>
        public AtomFeed ReadFeed(SDataBaseRequest request, ref string eTag)
        {
            Guard.ArgumentNotNull(request, "request");

            try
            {
                var requestUrl = request.ToString();
                var operation = new RequestOperation(HttpMethod.Get) {ETag = eTag};
                var response = ExecuteRequest(requestUrl, operation, MediaType.Atom);
                eTag = response.ETag;
                return (AtomFeed) response.Content;
            }
            catch (WebException ex)
            {
                throw new SDataServiceException(ex);
            }
            catch (Exception ex)
            {
                throw new SDataClientException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Reads resource information from the data source based on the URL.
        /// </summary>
        /// <param name="request">Request for the syndication resource to get information for.</param>
        /// <returns>An <see cref="AtomEntry"/> populated with the specified resources' information from the data source.</returns>
        public AtomEntry ReadEntry(SDataBaseRequest request)
        {
            return ReadEntry(request, null);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="request"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        public AtomEntry ReadEntry(SDataBaseRequest request, AtomEntry entry)
        {
            Guard.ArgumentNotNull(request, "request");

            try
            {
                var requestUrl = request.ToString();
                var eTag = entry != null ? entry.GetSDataHttpETag() : null;

                if (BatchProcess.Instance.Requests.Count > 0)
                {
                    var batchRequest = new SDataBatchRequestItem
                                       {
                                           Url = requestUrl,
                                           Method = HttpMethod.Get,
                                           ETag = eTag
                                       };
                    BatchProcess.Instance.AddToBatch(batchRequest);
                    return null;
                }

                var operation = new RequestOperation(HttpMethod.Get) {ETag = eTag};
                var response = ExecuteRequest(requestUrl, operation, MediaType.AtomEntry);
                entry = (AtomEntry) response.Content;

                if (!string.IsNullOrEmpty(response.ETag))
                {
                    entry.SetSDataHttpETag(response.ETag);
                }

                return entry;
            }
            catch (WebException ex)
            {
                throw new SDataServiceException(ex);
            }
            catch (Exception ex)
            {
                throw new SDataClientException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Reads xsd from a $schema request
        /// </summary>
        /// <param name="request">url for the syndication resource to get information for.</param>
        /// <returns>XmlSchema </returns>
        public XmlSchema ReadSchema(SDataResourceSchemaRequest request)
        {
            Guard.ArgumentNotNull(request, "request");

            try
            {
                var requestUrl = request.ToString();
                var operation = new RequestOperation(HttpMethod.Get);
                var response = ExecuteRequest(requestUrl, operation, MediaType.Xml);

                using (var reader = new StringReader((string) response.Content))
                {
                    return XmlSchema.Read(reader, null);
                }
            }
            catch (WebException ex)
            {
                throw new SDataServiceException(ex);
            }
            catch (Exception ex)
            {
                throw new SDataClientException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Updates information about a syndication resource in the data source.
        /// </summary>
        /// <param name="request">The url from the syndication data source for the resource to be updated.</param>
        /// <param name="entry">
        ///     An object that implements the <see cref="ISyndicationResource"/> interface that represents the updated information for the resource.
        /// </param>
        public AtomEntry UpdateEntry(SDataBaseRequest request, AtomEntry entry)
        {
            Guard.ArgumentNotNull(request, "request");
            Guard.ArgumentNotNull(entry, "entry");

            try
            {
                var requestUrl = request.ToString();
                var eTag = entry.GetSDataHttpETag();

                if (BatchProcess.Instance.Requests.Count > 0)
                {
                    var batchRequest = new SDataBatchRequestItem
                                       {
                                           Url = requestUrl,
                                           Method = HttpMethod.Put,
                                           Entry = entry,
                                           ETag = eTag
                                       };
                    BatchProcess.Instance.AddToBatch(batchRequest);
                    return null;
                }

                var operation = new RequestOperation(HttpMethod.Put, entry) {ETag = eTag};
                var response = ExecuteRequest(requestUrl, operation, MediaType.AtomEntry);
                entry = (AtomEntry) response.Content;

                if (!string.IsNullOrEmpty(response.ETag))
                {
                    entry.SetSDataHttpETag(response.ETag);
                }

                return entry;
            }
            catch (WebException ex)
            {
                throw new SDataServiceException(ex);
            }
            catch (Exception ex)
            {
                throw new SDataClientException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public SDataService()
            : this(null)
        {
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="url"></param>
        public SDataService(string url)
            : this(url, null, null)
        {
        }

        /// <summary>
        /// Constructor with pre-set url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="userName">user name used for credentials</param>
        /// <param name="password">password for user</param>
        public SDataService(string url, string userName, string password)
        {
            _uri = url != null
                       ? new SDataUri(url)
                       : new SDataUri
                         {
                             Server = "sdata",
                             Product = "-",
                             Contract = "-",
                             CompanyDataset = "-"
                         };
            UserName = userName;
            Password = password;
            Timeout = 120000;
            UserAgent = "Sage";
        }

        /// <summary>
        /// Initializes the <see cref="SDataService"/> 
        /// </summary>
        /// <remarks>Set the User Name and Password to authenticate with and build the url</remarks>
        [Obsolete("Explicit initialization is no longer required.")]
        public void Initialize()
        {
        }

        private SDataResponse ExecuteRequest(string url, RequestOperation operation, MediaType? accept)
        {
            var request = new SDataRequest(url, operation)
                          {
                              Accept = accept,
                              UserName = UserName,
                              Password = Password,
                              Timeout = Timeout,
                              Cookies = Cookies,
                              UserAgent = UserAgent
                          };
            return request.GetResponse();
        }
    }
}