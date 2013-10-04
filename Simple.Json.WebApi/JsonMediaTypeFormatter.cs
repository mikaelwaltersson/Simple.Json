using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json.WebApi
{
    public class JsonMediaTypeFormatter : MediaTypeFormatter
    {
        IJsonSerializer serializer = JsonSerializer.Default;

        public JsonMediaTypeFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/json"));

            SupportedEncodings.Add(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
            SupportedEncodings.Add(new UnicodeEncoding(bigEndian: false, byteOrderMark: true, throwOnInvalidBytes: true));
        }

        public IJsonSerializer Serializer
        {
            get { return serializer; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                serializer =  value;
            }
        }

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (readStream == null)
                throw new ArgumentNullException("readStream");

            try
            {
                return Task.FromResult(ReadFromStream(type, readStream, content, formatterLogger));
            }
            catch (Exception e)
            {
                return TaskResults.FromError<object>(e);
            }
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (writeStream == null)
                throw new ArgumentNullException("writeStream");

            try
            {
                WriteToStream(type, value, writeStream, content);
                
                return TaskResults.Completed();
            }
            catch (Exception e)
            {
                return TaskResults.FromError(e);
            }
        }

        public override bool CanReadType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return true;
        }

        public override bool CanWriteType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return true;
        }

        object ReadFromStream(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            var contentHeaders = GetContentHeaders(content);

            if (contentHeaders != null && contentHeaders.ContentLength == 0)            
                return GetDefaultValueForType(type);            

            var effectiveEncoding = SelectCharacterEncoding(contentHeaders);

            using (var reader = new StreamReader(readStream, effectiveEncoding))
            {
                return Serializer.ParseJson(reader, type);
            }
        }   

        void WriteToStream(Type type, object value, Stream writeStream, HttpContent content)
        {
            var effectiveEncoding = SelectCharacterEncoding(GetContentHeaders(content));

            using (var writer = new StreamWriter(writeStream, effectiveEncoding))
            {
                Serializer.ToJson(writer, value, type, false);
            }
        }

        static HttpContentHeaders GetContentHeaders(HttpContent content)
        {
            return content == null ? null : content.Headers;
        }


    }
}
