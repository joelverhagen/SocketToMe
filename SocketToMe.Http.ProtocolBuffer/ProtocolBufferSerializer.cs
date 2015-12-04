using System.IO;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Knapcode.SocketToMe.Http.ProtocolBuffer
{
    public interface IProtocolBufferSerializer
    {
        void Serialize<T>(Stream destination, T input);
        T Deserialize<T>(Stream source);
    }

    public class ProtocolBufferSerializer : IProtocolBufferSerializer
    {
        public static readonly TypeModel TypeModel;

        static ProtocolBufferSerializer()
        {
            var runtimeTypeModel = TypeModel.Create();

            var headerType = runtimeTypeModel.Add(typeof(HttpHeader), false);
            headerType.AddField(1, nameof(HttpHeader.Name)).IsRequired = true;
            headerType.AddField(2, nameof(HttpHeader.Value)).IsRequired = true;

            var requestType = runtimeTypeModel.Add(typeof(HttpRequest), false);
            requestType.AddField(1, nameof(HttpRequest.Method)).IsRequired = true;
            requestType.AddField(2, nameof(HttpRequest.Url)).IsRequired = true;
            requestType.AddField(3, nameof(HttpRequest.Version)).IsRequired = true;
            requestType.AddField(4, nameof(HttpRequest.Headers)).IsRequired = true;
            requestType.AddField(5, nameof(HttpRequest.HasContent)).IsRequired = true;

            var responseType = runtimeTypeModel.Add(typeof(HttpResponse), false);
            responseType.AddField(1, nameof(HttpResponse.Version)).IsRequired = true;
            responseType.AddField(2, nameof(HttpResponse.StatusCode)).IsRequired = true;
            responseType.AddField(3, nameof(HttpResponse.ReasonPhrease)).IsRequired = true;
            responseType.AddField(4, nameof(HttpResponse.Headers)).IsRequired = true;
            responseType.AddField(5, nameof(HttpResponse.HasContent)).IsRequired = true;

            var responseOrExceptionType = runtimeTypeModel.Add(typeof(HttpResponseOrException), false);
            responseOrExceptionType.AddField(1, nameof(HttpResponseOrException.Response)).IsRequired = false;
            responseOrExceptionType.AddField(2, nameof(HttpResponseOrException.ExceptionString)).IsRequired = false;

            TypeModel = runtimeTypeModel.Compile();
        }

        public void Serialize<T>(Stream destination, T input)
        {
            TypeModel.SerializeWithLengthPrefix(destination, input, typeof(T), PrefixStyle.Fixed32BigEndian, -1);
        }

        public T Deserialize<T>(Stream source)
        {
            return (T)TypeModel.DeserializeWithLengthPrefix(source, null, typeof(T), PrefixStyle.Fixed32BigEndian, -1);
        }
    }
}