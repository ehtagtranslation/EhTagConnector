﻿using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using KVP = System.Collections.Generic.KeyValuePair<string, EhTagClient.Record>;

namespace EhTagClient
{
    [DebuggerDisplay(@"Namespace={Namespace} Count={Count}")]
    public class RecordDictionary
    {
        public RecordDictionary(Namespace ns, RepoClient repoClient)
        {
            Namespace = ns;
            FilePath = Path.Combine(repoClient.LocalPath, $"database/{Namespace.ToString().ToLower()}.md");
        }

        [JsonIgnore]
        public string FilePath { get; }

        public Namespace Namespace { get; }

        public FrontMatters FrontMatters { get; set; }

        [JsonIgnore]
        public string Prefix { get; set; }

        [JsonIgnore]
        public string Suffix { get; set; }

        public int Count => MapData.Count;

        public struct DataDic : IReadOnlyDictionary<string, Record>
        {
            private readonly RecordDictionary _Parent;

            internal DataDic(RecordDictionary parent) => _Parent = parent;

            public Record this[string key] => _Parent.Find(key, false);

            public IEnumerable<string> Keys
            {
                get
                {
                    foreach (var item in _Parent.MapData.Keys)
                    {
                        yield return item;
                    }
                }
            }
            public IEnumerable<Record> Values
            {
                get
                {
                    foreach (var item in Keys)
                    {
                        yield return this[item];
                    }
                }
            }
            public int Count => _Parent.Count;

            public bool ContainsKey(string key) => _Parent.MapData.ContainsKey(key);

            public bool TryGetValue(string key, out Record value)
            {
                value = _Parent.Find(key, false);
                return !(value is null);

            }

            public IEnumerator<KVP> GetEnumerator()
            {
                foreach (var item in _Parent.MapData.Values)
                {
                    yield return _Parent.RawData[item];
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public DataDic Data => new DataDic(this);

        [JsonIgnore]
        public List<KVP> RawData { get; } = new List<KVP>();

        private Dictionary<string, int> MapData { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public void Load()
        {
            RawData.Clear();

            var state = 0;
            var sep = default(string);
            var prefix = new StringBuilder();
            var suffix = new StringBuilder();
            var frontMatters = new StringBuilder();

            using (var sr = new StreamReader(FilePath))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    var record = Record.TryParse(line);

                    switch (state)
                    {
                        case 0:
                            prefix.Append(line);
                            prefix.Append('\n');
                            if (record.Key != null)
                            {
                                state = 1;
                                continue;
                            }
                            else if (line.All(c => c == '-'))
                            {
                                state = 1;
                                sep = line;
                                continue;
                            }
                            else
                                continue;
                        case 1:
                            prefix.Append(line);
                            prefix.Append('\n');
                            if (line == sep)
                            {
                                state = 2;
                                continue;
                            }
                            else
                            {
                                frontMatters.Append(line);
                                frontMatters.Append('\n');
                                continue;
                            }
                        case 2:
                            prefix.Append(line);
                            prefix.Append('\n');
                            if (record.Key != null)
                            {
                                state = 3;
                                continue;
                            }
                            else
                                continue;
                        case 3:
                            prefix.Append(line);
                            prefix.Append('\n');
                            if (record.Key is null)
                            {
                                state = 2;
                                continue;
                            }
                            else
                            {
                                state = 4;
                                continue;
                            }
                        case 4:
                            if (record.Key is null)
                            {
                                suffix.Append(line);
                                suffix.Append('\n');
                                state = 5;
                                continue;
                            }
                            else
                            {
                                RawData.Add(record);
                                continue;
                            }
                        default:
                            suffix.Append(line);
                            suffix.Append('\n');
                            continue;
                    }
                }
            }

            Prefix = prefix.ToString();
            Suffix = suffix.ToString();
            FrontMatters = FrontMatters.Parse(frontMatters.ToString());

            MapData.Clear();

            var i = 0;
            foreach (var item in RawData)
            {
                if (!string.IsNullOrWhiteSpace(item.Key))
                    MapData[item.Key] = i;
                i++;
            }
        }

        public void Render()
        {
            Context.Namespace = Namespace;
            foreach (var item in RawData)
            {
                if (item.Key is null)
                    continue;
                item.Value.Render(item.Key);
            }
        }

        public void Save()
        {
            Context.Namespace = Namespace;
            using (var sw = new StreamWriter(FilePath) { NewLine = "\n" })
            {
                sw.Write(Prefix);
                foreach (var item in RawData)
                {
                    if (item.Key is null)
                        continue;
                    sw.WriteLine(item.Value.ToString(item.Key));
                }
                sw.Write(Suffix);
            }
        }

        public Record Find(string key, bool skipRender = false)
        {
            if (!MapData.TryGetValue(key, out var index))
                return null;
            var record = RawData[index].Value;
            if (!skipRender)
            {
                Context.Namespace = Namespace;
                record.Render(key);
            }
            return record;
        }

        public void Add(string key, Record record)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (record is null)
                throw new ArgumentNullException(nameof(record));
            _Add(key, record);
        }

        private void _Add(string key, Record record)
        {
            MapData.Add(key, RawData.Count);
            RawData.Add(KeyValuePair.Create(key, record));
        }

        public Record AddOrReplace(string key, Record record)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (record is null)
                throw new ArgumentNullException(nameof(record));

            if (MapData.TryGetValue(key, out var index))
            {
                var old = RawData[index];
                RawData[index] = KeyValuePair.Create(key, record);
                return old.Value;
            }
            else
            {
                _Add(key, record);
                return null;
            }
        }

        public void Rename(string oldKey, string newKey)
        {
            if (string.IsNullOrEmpty(oldKey))
                throw new ArgumentNullException(nameof(oldKey));
            if (string.IsNullOrEmpty(newKey))
                throw new ArgumentNullException(nameof(newKey));
            var index = MapData[oldKey];
            MapData.Add(newKey, index);
            MapData.Remove(oldKey);
        }

        public bool Remove(string key, bool deleteTransation)
        {
            if (!MapData.TryGetValue(key, out var index))
                return false;
            MapData.Remove(key);
            RawData[index] = deleteTransation ? default : KeyValuePair.Create("", RawData[index].Value);

            return true;
        }
    }
}
