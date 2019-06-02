using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Confuser.Core;
using Confuser.Core.Services;
using Confuser.Renamer.Analyzers;
using dnlib.DotNet;

namespace Confuser.Renamer
{
    // Token: 0x02000064 RID: 100
    internal class NameService : INameService
    {
        // Token: 0x06000256 RID: 598 RVA: 0x0001F8B8 File Offset: 0x0001DAB8
        public NameService(ConfuserContext context)
        {
            this.context = context;
            this.storage = new VTableStorage(context.Logger);
            this.random = context.Registry.GetService<IRandomService>().GetRandomGenerator("Ki.Rename");
            this.nameSeed = this.random.NextBytes(20);
            this.Renamers = new List<IRenamer>
            {
                new InterReferenceAnalyzer(),
                new VTableAnalyzer(),
                new TypeBlobAnalyzer(),
                new ResourceAnalyzer(),
                new LdtokenEnumAnalyzer()
            };
        }

        // Token: 0x170000A7 RID: 167
        // (get) Token: 0x06000257 RID: 599 RVA: 0x00003419 File Offset: 0x00001619
        // (set) Token: 0x06000258 RID: 600 RVA: 0x00003421 File Offset: 0x00001621
        public IList<IRenamer> Renamers
        {
            get;
            private set;
        }

        // Token: 0x06000259 RID: 601 RVA: 0x0000342A File Offset: 0x0000162A
        public VTableStorage GetVTables()
        {
            return this.storage;
        }

        // Token: 0x0600025A RID: 602 RVA: 0x0001F978 File Offset: 0x0001DB78
        public bool CanRename(object obj)
        {
            if (obj is IDnlibDef)
            {
                if (this.analyze == null)
                {
                    this.analyze = this.context.Pipeline.FindPhase<AnalyzePhase>();
                }
                NameProtection prot = (NameProtection)this.analyze.Parent;
                ProtectionSettings parameters = ProtectionParameters.GetParameters(this.context, (IDnlibDef)obj);
                return parameters != null && parameters.ContainsKey(prot) && this.context.Annotations.Get<bool>(obj, NameService.CanRenameKey, true);
            }
            return false;
        }

        // Token: 0x0600025B RID: 603 RVA: 0x00003432 File Offset: 0x00001632
        public void SetCanRename(object obj, bool val)
        {
            this.context.Annotations.Set<bool>(obj, NameService.CanRenameKey, val);
        }

        // Token: 0x0600025C RID: 604 RVA: 0x0000344B File Offset: 0x0000164B
        public RenameMode GetRenameMode(object obj)
        {
            return this.context.Annotations.Get<RenameMode>(obj, NameService.RenameModeKey, RenameMode.Sequential);
        }

        // Token: 0x0600025D RID: 605 RVA: 0x00003465 File Offset: 0x00001665
        public void SetRenameMode(object obj, RenameMode val)
        {
            this.context.Annotations.Set<RenameMode>(obj, NameService.RenameModeKey, val);
        }

        // Token: 0x0600025E RID: 606 RVA: 0x0001F9F8 File Offset: 0x0001DBF8
        public void ReduceRenameMode(object obj, RenameMode val)
        {
            RenameMode original = this.GetRenameMode(obj);
            if (original < val)
            {
                this.context.Annotations.Set<RenameMode>(obj, NameService.RenameModeKey, val);
            }
        }

        // Token: 0x0600025F RID: 607 RVA: 0x0000347E File Offset: 0x0000167E
        public void AddReference<T>(T obj, INameReference<T> reference)
        {
            this.context.Annotations.GetOrCreate<List<INameReference>>(obj, NameService.ReferencesKey, (object key) => new List<INameReference>()).Add(reference);
        }

        // Token: 0x06000260 RID: 608 RVA: 0x0001FA28 File Offset: 0x0001DC28
        public void Analyze(IDnlibDef def)
        {
            if (this.analyze == null)
            {
                this.analyze = this.context.Pipeline.FindPhase<AnalyzePhase>();
            }
            this.SetOriginalName(def, def.Name);
            if (def is TypeDef)
            {
                this.GetVTables().GetVTable((TypeDef)def);
                this.SetOriginalNamespace(def, ((TypeDef)def).Namespace);
            }
            this.analyze.Analyze(this, this.context, ProtectionParameters.Empty, def, true);
        }

        // Token: 0x06000261 RID: 609 RVA: 0x0001FAB0 File Offset: 0x0001DCB0
        private void IncrementNameId()
        {
            for (int i = this.nameId.Length - 1; i >= 0; i--)
            {
                byte[] expr_19_cp_0 = this.nameId;
                int expr_19_cp_1 = i;
                expr_19_cp_0[expr_19_cp_1] += 1;
                if (this.nameId[i] != 0)
                {
                    return;
                }
            }
        }
        public static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ᅠᅠOPVWXYZ0123456789qwertyuiopasdfghjklzxxcvbnm,./;[]*^$&@$!|}{><?_+";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        // Token: 0x06000262 RID: 610 RVA: 0x0001FAF8 File Offset: 0x0001DCF8
        public string ObfuscateName(string name, RenameMode mode)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }
            if (mode == RenameMode.Empty)
            {
                return "";
            }
            if (mode == RenameMode.Debug)
            {
                return "_" + name;
            }
            byte[] hash = Utils.Xor(Utils.SHA1(Encoding.UTF8.GetBytes(name)), this.nameSeed);
            switch (mode)
            {
                case RenameMode.Empty:
                    return "";
                case RenameMode.Unicode:
                    return Utils.EncodeString(hash, NameService.unicodeCharset) + "‮";
                case RenameMode.ASCII:
                    return Utils.EncodeString(hash, NameService.asciiCharset);
                case RenameMode.Letters:
                    return Utils.EncodeString(hash, NameService.letterCharset);
                default:
                    switch (mode)
                    {
                        case RenameMode.Decodable:
                            {
                                if (this.nameMap1.ContainsKey(name))
                                {
                                    return this.nameMap1[name];
                                }
                                this.IncrementNameId();
                                string newName = "_" + Utils.EncodeString(hash, NameService.alphaNumCharset) + "_";
                                this.nameMap2[newName] = name;
                                this.nameMap1[name] = newName;
                                return newName;
                            }
                        case RenameMode.Sequential:
                            {
                                if (this.nameMap1.ContainsKey(name))
                                {
                                    return this.nameMap1[name];
                                }
                                this.IncrementNameId();
                                //string newName2 = "ᅠ" +Utils.EncodeString(this.nameId, NameService.unicodeCharset )+RandomString(name.Length + new Random().Next(5,10));
                                string newName2 = "Bed-Private-v8" + Utils.EncodeString(this.nameId, NameService.alphaNumCharset) + "Bed-Private-v8" + Utils.EncodeString(this.nameId, NameService.alphaNumCharset) + "Bed-Private-v8";
                                // string newName2 = "Kill-Yourself-<3" + Utils.EncodeString(this.nameId, NameService.alphaNumCharset) + "Kill-Yourself-<3" + Utils.EncodeString(this.nameId, NameService.alphaNumCharset) + Utils.EncodeString(this.nameId, NameService.alphaNumCharset) + "Kill-Yourself-<3" + Utils.EncodeString(this.nameId, NameService.alphaNumCharset) + Utils.EncodeString(this.nameId, NameService.alphaNumCharset) + "Kill-Yourself-<3" + Utils.EncodeString(this.nameId, NameService.alphaNumCharset) + Utils.EncodeString(this.nameId, NameService.alphaNumCharset) + "Kill-Yourself-<3" + Utils.EncodeString(this.nameId, NameService.alphaNumCharset);
                                this.nameMap2[newName2] = name;
                                this.nameMap1[name] = newName2;
                                return newName2;
                            }
                        case RenameMode.Reversible:
                            if (this.reversibleRenamer == null)
                            {
                                throw new ArgumentException("Password not provided for reversible renaming.");
                            }
                            return this.reversibleRenamer.Encrypt(name);
                        default:
                            throw new NotSupportedException("Rename mode '" + mode + "' is not supported.");
                    }
                    break;
            }
        }

        // Token: 0x06000263 RID: 611 RVA: 0x000034AD File Offset: 0x000016AD
        public string RandomName()
        {
            return this.RandomName(RenameMode.Sequential);
        }

        // Token: 0x06000264 RID: 612 RVA: 0x000034B7 File Offset: 0x000016B7
        public string RandomName(RenameMode mode)
        {
            return this.ObfuscateName(Utils.ToHexString(this.random.NextBytes(16)), mode);
        }

        // Token: 0x06000265 RID: 613 RVA: 0x000034D2 File Offset: 0x000016D2
        public void SetOriginalName(object obj, string name)
        {
            this.context.Annotations.Set<string>(obj, NameService.OriginalNameKey, name);
        }

        // Token: 0x06000266 RID: 614 RVA: 0x000034EB File Offset: 0x000016EB
        public void SetOriginalNamespace(object obj, string ns)
        {
            this.context.Annotations.Set<string>(obj, NameService.OriginalNamespaceKey, ns);
        }

        // Token: 0x06000267 RID: 615 RVA: 0x00003504 File Offset: 0x00001704
        public void RegisterRenamer(IRenamer renamer)
        {
            this.Renamers.Add(renamer);
        }

        // Token: 0x06000268 RID: 616 RVA: 0x00003512 File Offset: 0x00001712
        public T FindRenamer<T>()
        {
            return this.Renamers.OfType<T>().Single<T>();
        }

        // Token: 0x06000269 RID: 617 RVA: 0x0001FCA4 File Offset: 0x0001DEA4
        public void MarkHelper(IDnlibDef def, IMarkerService marker, ConfuserComponent parentComp)
        {
            if (marker.IsMarked(def))
            {
                return;
            }
            if (def is MethodDef)
            {
                MethodDef method = (MethodDef)def;
                method.Access = MethodAttributes.Assembly;
                if (!method.IsSpecialName && !method.IsRuntimeSpecialName && !method.DeclaringType.IsDelegate())
                {
                    method.Name = this.RandomName();
                }
            }
            else if (def is FieldDef)
            {
                FieldDef field = (FieldDef)def;
                field.Access = FieldAttributes.Assembly;
                if (!field.IsSpecialName && !field.IsRuntimeSpecialName)
                {
                    field.Name = this.RandomName();
                }
            }
            else if (def is TypeDef)
            {
                TypeDef type = (TypeDef)def;
                type.Visibility = ((type.DeclaringType == null) ? TypeAttributes.NotPublic : TypeAttributes.NestedAssembly);
                type.Namespace = "";
                if (!type.IsSpecialName && !type.IsRuntimeSpecialName)
                {
                    type.Name = this.RandomName();
                }
            }
            this.SetCanRename(def, false);
            this.Analyze(def);
            marker.Mark(def, parentComp);
        }

        // Token: 0x0600026A RID: 618 RVA: 0x00003524 File Offset: 0x00001724
        public RandomGenerator GetRandom()
        {
            return this.random;
        }

        // Token: 0x0600026B RID: 619 RVA: 0x0000352C File Offset: 0x0000172C
        public IList<INameReference> GetReferences(object obj)
        {
            return this.context.Annotations.GetLazy<List<INameReference>>(obj, NameService.ReferencesKey, (object key) => new List<INameReference>());
        }

        // Token: 0x0600026C RID: 620 RVA: 0x00003561 File Offset: 0x00001761
        public string GetOriginalName(object obj)
        {
            return this.context.Annotations.Get<string>(obj, NameService.OriginalNameKey, "");
        }

        // Token: 0x0600026D RID: 621 RVA: 0x0000357E File Offset: 0x0000177E
        public string GetOriginalNamespace(object obj)
        {
            return this.context.Annotations.Get<string>(obj, NameService.OriginalNamespaceKey, "");
        }

        // Token: 0x0600026E RID: 622 RVA: 0x0000359B File Offset: 0x0000179B
        public ICollection<KeyValuePair<string, string>> GetNameMap()
        {
            return this.nameMap2;
        }

        // Token: 0x040004D8 RID: 1240
        private static readonly object CanRenameKey = new object();

        // Token: 0x040004D9 RID: 1241
        private static readonly object RenameModeKey = new object();

        // Token: 0x040004DA RID: 1242
        private static readonly object ReferencesKey = new object();

        // Token: 0x040004DB RID: 1243
        private static readonly object OriginalNameKey = new object();

        // Token: 0x040004DC RID: 1244
        private static readonly object OriginalNamespaceKey = new object();

        // Token: 0x040004DD RID: 1245
        private readonly ConfuserContext context;

        // Token: 0x040004DE RID: 1246
        private readonly byte[] nameSeed;

        // Token: 0x040004DF RID: 1247
        private readonly RandomGenerator random;

        // Token: 0x040004E0 RID: 1248
        private readonly VTableStorage storage;

        // Token: 0x040004E1 RID: 1249
        private AnalyzePhase analyze;

        // Token: 0x040004E2 RID: 1250
        private readonly byte[] nameId = new byte[8];

        // Token: 0x040004E3 RID: 1251
        private readonly Dictionary<string, string> nameMap1 = new Dictionary<string, string>();

        // Token: 0x040004E4 RID: 1252
        private readonly Dictionary<string, string> nameMap2 = new Dictionary<string, string>();

        // Token: 0x040004E5 RID: 1253
        internal ReversibleRenamer reversibleRenamer;

        // Token: 0x040004E6 RID: 1254
        private static readonly char[] asciiCharset = (from ord in Enumerable.Range(32, 95)
                                                       select (char)ord).Except(new char[]
        {
            '.'
        }).ToArray<char>();

        // Token: 0x040004E7 RID: 1255
        private static readonly char[] letterCharset = Enumerable.Range(0, 26).SelectMany((int ord) => new char[]
        {
            (char)(97 + ord),
            (char)(65 + ord)
        }).ToArray<char>();

        // Token: 0x040004E8 RID: 1256
        private static readonly char[] alphaNumCharset = Enumerable.Range(0, 26).SelectMany((int ord) => new char[]
        {
            (char)(97 + ord),
            (char)(65 + ord)
        }).Concat(from ord in Enumerable.Range(0, 10)
                  select (char)(48 + ord)).ToArray<char>();

        // Token: 0x040004E9 RID: 1257
        private static readonly char[] unicodeCharset = new char[0].Concat(from ord in Enumerable.Range(8203, 5)
                                                                           select (char)ord).Concat(from ord in Enumerable.Range(8233, 6)
                                                                                                    select (char)ord).Concat(from ord in Enumerable.Range(8298, 6)
                                                                                                                             select (char)ord).Except(new char[]
        {
            '\u2029'
        }).ToArray<char>();
    }
}
