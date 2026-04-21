using System.Globalization;
using Newtonsoft.Json;

namespace com.Plapamaru.Utilities
{
    public static class JsonExtensions
    {
        public static T AsModel<T>(this string input, params JsonConverter[] converters)
        {
            return JsonConvert.DeserializeObject<T>(input, converters);
        }

        public static string AsJson(this object input, params JsonConverter[] converters)
        {
            return JsonConvert.SerializeObject(input, converters);
        }

        public static string AsPrettyJson(this object input, params JsonConverter[] converters)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                Converters = converters,
                Formatting = Formatting.Indented,
                Culture = CultureInfo.InvariantCulture
            };

            return JsonConvert.SerializeObject(input, settings);
        }
    }
}