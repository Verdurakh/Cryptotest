using System.Text.Json.Serialization;

namespace CryptoTest.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderTypeEnum
{
    Buy = 1,
    Sell = 2
}