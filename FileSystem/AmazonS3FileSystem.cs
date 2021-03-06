﻿namespace AST.S3.FileSystem
{
    using System.IO;

    using Amazon;
    using Amazon.S3;
    using Amazon.S3.Model;

    public class AmazonS3FileSystem
    {
        private readonly IAmazonS3 _client;
        private string _bucketName;

        #region Constructor

        public AmazonS3FileSystem(string accessKey, string secretKey, string bucketName)
        {
            _client = AWSClientFactory.CreateAmazonS3Client(accessKey, secretKey);
            _bucketName = bucketName;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks whether a file exists
        /// </summary>
        /// <param name="path">Web path to file's folder</param>
        /// <param name="fileName">File name</param>
        /// <returns>True if file exists, false if not</returns>
        public bool DoesFileExist(string path, string fileName)
        {
            try
            {
                var response = _client.GetObjectMetadata(
                                        new GetObjectMetadataRequest()
                                        {
                                            BucketName = _bucketName,
                                            Key = GetKey(path, fileName)
                                        });

                return true;
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound || ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return false;
                }

                // Status wasn't not found, so throw the exception
                throw;
            }
        }

        /// <summary>
        /// Saves an HTTP POSTed file
        /// </summary>
        /// <param name="localFile">HTTP POSTed file</param>
        /// <param name="path">Web path to file's folder</param>
        /// <param name="fileName">File name</param>
        public void SaveFile(string localFile, string path, string fileName)
        {
            // Check if the local file exsit
            if (!File.Exists(localFile))
            {
                return;
            }

            // Prepare put request            
            var request = new PutObjectRequest()
                                {
                                    BucketName = _bucketName,
                                    CannedACL = S3CannedACL.PublicRead,
                                    FilePath = localFile,
                                    Key = GetKey(path,fileName)
                                }; // NOTE: timeout property not found during AWSSDK v1 to v2 code changes

            // Put file
            var response = _client.PutObject(request);
            //response.Dispose(); // NOTE: Dispose() not valid during AWSSDK v1 to v2 code changes
        }

        /// <summary>
        /// Deletes a file
        /// </summary>
        /// <param name="path">Web path to file's folder</param>
        /// <param name="fileName">File name</param>
        public void DeleteFile(string path, string fileName)
        {
            // Prepare delete request
            var request = new DeleteObjectRequest()
                {
                    BucketName = _bucketName,
                    Key = GetKey(path, fileName)
                };

            // Delete file
            var response = _client.DeleteObject(request);
            //response.Dispose();
        }

        /// <summary>
        /// Delete a folder
        /// </summary>
        /// <param name="prefix">prefix</param>
        public void DeleteFolder(string prefix)
        {
            // Get all object with specified prefix
            var listRequest = new ListObjectsRequest()
                                    {
                                        BucketName = _bucketName,
                                        Prefix = prefix
                                    };

            var deleteRequest = new DeleteObjectsRequest();
            deleteRequest.BucketName = _bucketName;

            do
            {
                ListObjectsResponse listResponse = _client.ListObjects(listRequest);

                // Add all object with specified prefix to delete request.
                foreach (S3Object entry in listResponse.S3Objects)
                {
                    deleteRequest.AddKey(entry.Key);
                }

                if (listResponse.IsTruncated)
                {
                    listRequest.Marker = listResponse.NextMarker;
                }
                else
                {
                    listRequest = null;
                }
            }
            while (listRequest != null);

            // Delete all the object with specified prefix.
            if (deleteRequest.Objects.Count > 0) // changes Keys to Object during AWSSDK v1 to v2 code changes
            {
                var deleteResponse = _client.DeleteObjects(deleteRequest);
                //deleteResponse.Dispose();
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Helper to construct object key from path
        /// </summary>
        /// <param name="path">Web path to file's folder</param>
        /// <param name="fileName">File name</param>
        /// <returns>Key value</returns>
        private string GetKey(string path, string fileName)
        {
            // Ensure path is relative to root
            if (path.StartsWith("/"))
            {
                path = path.Substring(1);
            }

            return Path.Combine(path, fileName);
        }

        #endregion
    }
}