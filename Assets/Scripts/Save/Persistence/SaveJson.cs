using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

// 저장 층의 JSON 규약. 로컬 파일과 서버 요청/응답이 전부 이 설정을 사용.
//
// 멤버 이름은 camelCase(서버 Jackson과 같게),
// 딕셔너리 키는 그대로 - CamelCasePropertyNamesContractResolver를 그냥 쓰면 스탯 키까지 바뀜.
public static class SaveJson
{
    public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy
            {
                ProcessDictionaryKeys = false,
                OverrideSpecifiedNames = true,
            },
        },
        NullValueHandling = NullValueHandling.Include,
    };

    // 응답 안에 그대로 실려 온 JSON(스냅샷)을 같은 규약으로 되읽을 때.
    public static readonly JsonSerializer Serializer = JsonSerializer.Create(Settings);

    public static string Serialize(object value) =>
        JsonConvert.SerializeObject(value, Settings);

    public static string SerializePretty(object value) =>
        JsonConvert.SerializeObject(value, Formatting.Indented, Settings);

    public static T Deserialize<T>(string json) =>
        JsonConvert.DeserializeObject<T>(json, Settings);
}