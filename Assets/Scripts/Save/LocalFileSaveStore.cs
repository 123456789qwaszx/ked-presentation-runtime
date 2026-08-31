using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ked.Save
{
    // Application.persistentDataPath/saves/slot{n}.json (M7-2).
    //
    // 경로를 생성자에서 문자열로 받는 이유: persistentDataPath는 메인 스레드의 Unity API라
    // 조립 시점(VNAppBootstrap.Awake)에 한 번 읽어 주입한다. 이 클래스는 Unity에 안 기대고,
    // 나중에 에디터 테스트가 임시 폴더를 꽂을 수 있다.
    public sealed class LocalFileSaveStore : ISaveStore
    {
        private readonly string _directory;

        public LocalFileSaveStore(string directory)
        {
            _directory = directory;
        }

        private string PathOf(int slotNo) =>
            Path.Combine(_directory, $"slot{slotNo}.json");

        public void Save(LocalSaveFile save)
        {
            if (save == null)
                throw new ArgumentNullException(nameof(save));

            AtomicFile.WriteAllText(PathOf(save.SlotNo), SaveJson.SerializePretty(save));
        }

        public LocalSaveFile Load(int slotNo)
        {
            string json = AtomicFile.ReadAllTextOrNull(PathOf(slotNo));

            if (json == null)
                return null;

            try
            {
                return SaveJson.Deserialize<LocalSaveFile>(json);
            }
            catch (Exception error)
            {
                // 깨진 세이브는 "없음"이다 — 여기서 던지면 게임이 아예 못 뜬다.
                // 원인은 로그로 남긴다(파일은 그대로 두므로 사람이 열어 볼 수 있다).
                Debug.LogWarning($"[저장] slot{slotNo}.json 을 읽지 못했다 — 새 게임으로 취급.\n{error}");
                return null;
            }
        }

        public IReadOnlyList<LocalSaveFile> ListAll()
        {
            var result = new List<LocalSaveFile>();

            if (!Directory.Exists(_directory))
                return result;

            foreach (string file in Directory.GetFiles(_directory, "slot*.json"))
            {
                // 파일명이 아니라 내용의 SlotNo를 믿는다 — 어차피 읽어야 하고, 둘이 갈리면
                // 내용이 진실이다(파일명은 사람이 복사하며 바꿀 수 있다).
                string json = AtomicFile.ReadAllTextOrNull(file);

                if (json == null)
                    continue;

                try
                {
                    LocalSaveFile save = SaveJson.Deserialize<LocalSaveFile>(json);

                    if (save != null)
                        result.Add(save);
                }
                catch (Exception)
                {
                    // 목록에서 깨진 파일 하나가 전체를 막지 않는다. Load가 같은 파일을
                    // 만나면 거기서 경고를 남긴다.
                }
            }

            result.Sort((a, b) => a.SlotNo.CompareTo(b.SlotNo));

            return result;
        }
    }
}
