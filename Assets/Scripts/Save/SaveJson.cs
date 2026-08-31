using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Ked.Save
{
    // 저장 층의 JSON 규약 한 곳 (M7).
    //
    // 로컬 파일과 서버 요청·응답이 전부 이 설정 하나를 쓴다 — 서버(Jackson)의 이름 규칙이
    // camelCase라서 C#의 PascalCase 멤버를 내보낼 때 첫 글자를 내린다.
    //
    // ProcessDictionaryKeys = false가 이 클래스의 존재 이유다.
    // CamelCasePropertyNamesContractResolver를 그냥 쓰면 **딕셔너리 키까지** camelCase로
    // 바꾼다 — 스탯 키("Trust" 등)는 콘텐츠가 정한 식별자라 한 글자만 바뀌어도 다른 스탯이
    // 된다. 멤버 이름만 내리고 데이터 키는 손대지 않는다.
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
            // null 필드도 내보낸다. 서버는 null과 "없음"을 같게 접지만(choicesOrEmpty),
            // baseRevision처럼 null이 뜻을 갖는 자리가 있어 명시가 안전하다.
            NullValueHandling = NullValueHandling.Include,
        };

        public static string Serialize(object value) =>
            JsonConvert.SerializeObject(value, Settings);

        // 로컬 파일용 — 사람이 열어 볼 일이 많아 들여쓰기를 넣는다.
        public static string SerializePretty(object value) =>
            JsonConvert.SerializeObject(value, Formatting.Indented, Settings);

        public static T Deserialize<T>(string json) =>
            JsonConvert.DeserializeObject<T>(json, Settings);
    }
}
