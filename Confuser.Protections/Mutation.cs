  private static Random rnd = new Random();
        public static List<Instruction> instr = new List<Instruction>();
        public static bool CanObfuscateLDCI4(IList<Instruction> instructions, int i)
        {
            if (instructions[i + 1].GetOperand() != null)
                if (instructions[i + 1].Operand.ToString().Contains("bool"))
                    return false;
            if (instructions[i + 1].OpCode == OpCodes.Newobj)
                return false;
            if (instructions[i].GetLdcI4Value() == 0 || instructions[i].GetLdcI4Value() == 1)
                return false;


            return true;
        }

        public static void EmptyType(MethodDef method, ref int i)
        {
            if (method.Body.Instructions[i].IsLdcI4())
            {
                int operand = method.Body.Instructions[i].GetLdcI4Value();
                method.Body.Instructions[i].Operand = operand - Type.EmptyTypes.Length;
                method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
                method.Body.Instructions.Insert(i + 1, OpCodes.Ldsfld.ToInstruction(method.Module.Import(typeof(Type).GetField("EmptyTypes"))));
                method.Body.Instructions.Insert(i + 2, Instruction.Create(OpCodes.Ldlen));
                method.Body.Instructions.Insert(i + 3, Instruction.Create(OpCodes.Add));
            }
        }
        public static void DoubleParse(MethodDef method, ref int i)
        {
            if (method.Body.Instructions[i].IsLdcI4())
            {
                int operand = method.Body.Instructions[i].GetLdcI4Value();
                double n = RandomDouble(1.0, 1000.0);
                string converter = Convert.ToString(n);
                double nEw = double.Parse(converter);
                int conta = operand - (int)nEw;
                method.Body.Instructions[i].Operand = conta;
                method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
                method.Body.Instructions.Insert(i + 1, Instruction.Create(OpCodes.Ldstr, converter));
                method.Body.Instructions.Insert(i + 2, OpCodes.Call.ToInstruction(method.Module.Import(typeof(double).GetMethod("Parse", new Type[] { typeof(string) }))));
                method.Body.Instructions.Insert(i + 3, OpCodes.Conv_I4.ToInstruction());
                method.Body.Instructions.Insert(i + 4, Instruction.Create(OpCodes.Add));
            }
        }

        public static void Brs(MethodDef method)
        {
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                Instruction instr = method.Body.Instructions[i];
                if (instr.IsLdcI4())
                {
                    int operand = instr.GetLdcI4Value();
                    instr.OpCode = OpCodes.Ldc_I4;
                    instr.Operand = operand - 1;
                    int valor = rnd.Next(100, 500);
                    int valor2 = rnd.Next(1000, 5000);
                    method.Body.Instructions.Insert(i + 1, Instruction.CreateLdcI4(valor));
                    method.Body.Instructions.Insert(i + 2, Instruction.CreateLdcI4(valor2));
                    method.Body.Instructions.Insert(i + 3, Instruction.Create(OpCodes.Clt));
                    method.Body.Instructions.Insert(i + 4, Instruction.Create(OpCodes.Conv_I4));
                    method.Body.Instructions.Insert(i + 5, Instruction.Create(OpCodes.Add));
                    i += 5;
                }
            }
        }
            public static string RandomString(int length, string chars)
        {
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[rnd.Next(s.Length)]).ToArray());
        }
        public static void resarm(MethodDef method, CEContext context)
        {
            TypeDef rtType = context.Context.Registry.GetService<IRuntimeService>().GetRuntimeType("Confuser.Runtime.ResMut");
            string nome = "CompleX-" + rnd.Next(1, int.MaxValue);
            IEnumerable<IDnlibDef> members = InjectHelper.Inject(rtType, method.Module.GlobalType, method.Module);
            MethodDef cctor = method.Module.GlobalType.FindStaticConstructor();
            var init = (MethodDef)members.Single(adedanha => adedanha.Name == "Initialize");
            context.Name.MarkHelper(init, context.Marker, context.Protection);
            MutationHelper.InjectString(init, "a", nome);
            string list = "";
            int b = 0;
            for (int i = 0; i < method.Body.Instructions.Count; ++i)
            {
                if (method.Body.Instructions[i].IsLdcI4())
                {
                    int y = rnd.Next(1, 200);
                    list += (method.Body.Instructions[i].GetLdcI4Value() ^ y).ToString() + $" ^ {y}\r\n";
                    method.Body.Instructions[i] = OpCodes.Ldc_I4.ToInstruction(b++);
                    method.Body.Instructions.Insert(i + 1, OpCodes.Call.ToInstruction(init));
                    i++;
                }
            }
            method.Module.Resources.Add(new EmbeddedResource(nome, System.Text.UTF8Encoding.UTF8.GetBytes(list)));
        }

        private static void Calc(MethodDef method)
        {
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                if (method.Body.Instructions[i].IsLdcI4())
                {
                    int op = method.Body.Instructions[i].GetLdcI4Value();
                    int newvalue = rnd.Next(-100, 10000);
                    switch (rnd.Next(1, 4))
                    {
                        case 1:
                            method.Body.Instructions[i].Operand = op - newvalue;
                            method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4.ToInstruction(newvalue));
                            method.Body.Instructions.Insert(i + 2, OpCodes.Add.ToInstruction());
                            i += 2;
                            break;
                        case 2:
                            method.Body.Instructions[i].Operand = op + newvalue;
                            method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4.ToInstruction(newvalue));
                            method.Body.Instructions.Insert(i + 2, OpCodes.Sub.ToInstruction());
                            i += 2;
                            break;
                        case 3:
                            method.Body.Instructions[i].Operand = op ^ newvalue;
                            method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4.ToInstruction(newvalue));
                            method.Body.Instructions.Insert(i + 2, OpCodes.Xor.ToInstruction());
                            i += 2;
                            break;
                        case 4:
                            int operand = method.Body.Instructions[i].GetLdcI4Value();
                            method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
                            method.Body.Instructions[i].Operand = operand - 1;
                            int valor = rnd.Next(100, 500);
                            int valor2 = rnd.Next(1000, 5000);
                            method.Body.Instructions.Insert(i + 1, Instruction.CreateLdcI4(valor));
                            method.Body.Instructions.Insert(i + 2, Instruction.CreateLdcI4(valor2));
                            method.Body.Instructions.Insert(i + 3, Instruction.Create(OpCodes.Clt));
                            method.Body.Instructions.Insert(i + 4, Instruction.Create(OpCodes.Conv_I4));
                            method.Body.Instructions.Insert(i + 5, Instruction.Create(OpCodes.Add));
                            i += 5;
                            break;
                    }
                }
            }
        }
        private static double RandomDouble(double min, double max)
        {
            return new Random().NextDouble() * (max - min) + min;
        }
        private static void Mutation(MethodDef method)
        {
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                if (method.Body.Instructions[i].IsLdcI4())
                {
                    int op = method.Body.Instructions[i].GetLdcI4Value();
                    int newvalue = rnd.Next(3, 12);
                    switch (rnd.Next(1, 11))
                    {
                        case 1:
                            method.Body.Instructions[i].OpCode = OpCodes.Ldstr;
                            method.Body.Instructions[i].Operand = Convert.ToString(op);
                            method.Body.Instructions.Insert(i + 1, OpCodes.Call.ToInstruction(method.Module.Import(typeof(Int32).GetMethod("Parse", new Type[] { typeof(string) }))));
                            i += 1;
                            break;
                        case 2:
                            method.Body.Instructions[i].Operand = op - newvalue;
                            method.Body.Instructions.Insert(i + 1, OpCodes.Ldstr.ToInstruction(RandomString(newvalue, "畹畞疲疷疹痲痹痹瘕番畐畞畵畵畲畲蘽蘐藴虜蘞虢謊謁")));
                            method.Body.Instructions.Insert(i + 2, OpCodes.Ldlen.ToInstruction());
                            method.Body.Instructions.Insert(i + 3, OpCodes.Add.ToInstruction());
                            i += 3;
                            break;
                        case 3:
                            method.Body.Instructions[i].Operand = op + 4;
                            switch (rnd.Next(1, 4))
                            {
                                case 1:
                                    method.Body.Instructions.Insert(i + 1, new Instruction(OpCodes.Sizeof, method.Module.Import(typeof(System.Security.SecurityZone))));
                                    break;
                                case 2:
                                    method.Body.Instructions.Insert(i + 1, new Instruction(OpCodes.Sizeof, method.Module.Import(typeof(System.Security.SecurityCriticalScope))));
                                    break;
                                case 3:
                                    method.Body.Instructions.Insert(i + 1, new Instruction(OpCodes.Sizeof, method.Module.Import(typeof(System.Security.HostSecurityManagerOptions))));
                                    break;

                            }
                            method.Body.Instructions.Insert(i + 2, OpCodes.Sub.ToInstruction());
                            i += 2;
                            break;
                        case 4:
                            FloorReplacer(method, method.Body.Instructions[i], ref i);
                            i += 2;
                            break;
                        case 5:
                            CeilingReplacer(method, method.Body.Instructions[i], ref i);
                            i += 2;
                            break;
                        case 6:
                            RoundReplacer(method, method.Body.Instructions[i], ref i);
                            i += 2;
                            break;
                        case 7:
                            SqrtReplacer(method, method.Body.Instructions[i], ref i);
                            i += 2;
                            break;
                        case 8:
                            StructGenerator(method, ref i);
                            i += 3;
                            break;
                        case 9:
                            EmptyType(method, ref i);
                            i += 3;
                            break;
                        case 10:
                            DoubleParse(method, ref i);
                            i += 4;
                            break;
                    }
                }
            }
        }
        public static int[] rndsizevalues = new int[] { 1, 2, 4, 8, 12, 16 };
        public static Dictionary<int, Tuple<TypeDef, int>> Dick = new Dictionary<int, Tuple<TypeDef, int>>();
        static int abc = 0;
        public static void StructGenerator(MethodDef method, ref int i)
        {
            if (method.Body.Instructions[i].IsLdcI4())
            {
                ITypeDefOrRef valueTypeRef = new Importer(method.Module).Import(typeof(System.ValueType));
                TypeDef structDef = new TypeDefUser(RandomString(rnd.Next(10, 30), "畹畞疲疷疹痲痹痹瘕番畐畞畵畵畲畲蘽蘐藴虜蘞虢謊謁abcdefghijlmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ01"), valueTypeRef);
                Tuple<TypeDef, int> outTuple;
                structDef.ClassLayout = new ClassLayoutUser(1, 0);
                structDef.Attributes |= TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.Public;
                List<Type> retList = new List<Type>();
                int rand = rndsizevalues[rnd.Next(0, 6)];
                retList.Add(GetType(rand));
                retList.ForEach(x => structDef.Fields.Add(new FieldDefUser(RandomString(rnd.Next(10, 30), "畹畞疲疷疹痲痹痹瘕番畐畞畵畵畲畲蘽蘐藴虜蘞虢謊謁abcdefghijlmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"), new FieldSig(new Importer(method.Module).Import(x).ToTypeSig()), FieldAttributes.Public)));
                int operand = method.Body.Instructions[i].GetLdcI4Value();
                if (abc < 25)
                {
                    method.Module.Types.Add(structDef);
                    Dick.Add(abc++, new Tuple<TypeDef, int>(structDef, rand));
                    int conta = operand - rand;
                    method.Body.Instructions[i].Operand = conta;
                    method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
                    method.Body.Instructions.Insert(i + 1, Instruction.Create(OpCodes.Sizeof, structDef));
                    method.Body.Instructions.Insert(i + 2, Instruction.Create(OpCodes.Add));
                }
                else
                {
                    Dick.TryGetValue(rnd.Next(1, 24), out outTuple);
                    int conta = operand - outTuple.Item2;
                    method.Body.Instructions[i].Operand = conta;
                    method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
                    method.Body.Instructions.Insert(i + 1, Instruction.Create(OpCodes.Sizeof, outTuple.Item1));
                    method.Body.Instructions.Insert(i + 2, Instruction.Create(OpCodes.Add));
                }
            }
        }
        private static Type GetType(int operand)
        {
            switch (operand)
            {
                case 1:
                    switch (rnd.Next(0, 3))
                    {
                        case 0: return typeof(Boolean);
                        case 1: return typeof(SByte);
                        case 2: return typeof(Byte);
                    }
                    break;
                case 2:
                    switch (rnd.Next(0, 3))
                    {
                        case 0: return typeof(Int16);
                        case 1: return typeof(UInt16);
                        case 2: return typeof(Char);
                    }
                    break;
                case 4:
                    switch (rnd.Next(0, 3))
                    {
                        case 0: return typeof(Int32);
                        case 1: return typeof(Single);
                        case 2: return typeof(UInt32);
                    }
                    break;
                case 8:
                    switch (rnd.Next(0, 5))
                    {
                        case 0: return typeof(DateTime);
                        case 1: return typeof(TimeSpan);
                        case 2: return typeof(Int64);
                        case 3: return typeof(Double);
                        case 4: return typeof(UInt64);
                    }
                    break;

                case 12: return typeof(ConsoleKeyInfo);

                case 16:
                    switch (rnd.Next(0, 2))
                    {
                        case 0: return typeof(Guid);
                        case 1: return typeof(Decimal);
                    }
                    break;
            }

            return null;
        }
        public static List<Type> CreateTypeList(int size, out int total)
        {
            List<Type> retList = new List<Type>();
            int t = 0;
            while (size != 0)
            {
                if (16 <= size)
                {
                    size -= 16;
                    t += 16;
                    retList.Add(GetType(16));
                }
                else if (12 <= size)
                {
                    size -= 12;
                    t += 12;
                    retList.Add(GetType(12));
                }
                else if (8 <= size)
                {
                    size -= 8;
                    t += 8;
                    retList.Add(GetType(8));
                }
                else if (4 <= size)
                {
                    size -= 4;
                    t += 4;

                    retList.Add(GetType(4));
                }
                else if (2 <= size)
                {
                    size -= 2;
                    t += 2;
                    retList.Add(GetType(2));
                }
                else if (1 <= size)
                {
                    size -= 1;
                    t += 1;
                    retList.Add(GetType(1));
                }
            }

            total = t;
            return retList;
        }
        public static string RandomOrNo()
        {
            string[] charsc = { "CausalityTraceLevel", "BitConverter", "UnhandledExceptionEventHandler", "PinnedBufferMemoryStream", "RichTextBoxScrollBars", "RichTextBoxSelectionAttribute", "RichTextBoxSelectionTypes", "RichTextBoxStreamType", "RichTextBoxWordPunctuations", "RightToLeft", "RTLAwareMessageBox", "SafeNativeMethods", "SaveFileDialog", "Screen", "ScreenOrientation", "ScrollableControl", "ScrollBar", "ScrollBarRenderer", "ScrollBars", "ScrollButton", "ScrollEventArgs", "ScrollEventHandler", "ScrollEventType", "ScrollOrientation", "ScrollProperties", "SearchDirectionHint", "SearchForVirtualItemEventArgs", "SearchForVirtualItemEventHandler", "SecurityIDType", "SelectedGridItemChangedEventArgs", "SelectedGridItemChangedEventHandler", "SelectionMode", "SelectionRange", "SelectionRangeConverter", "SendKeys", "Shortcut", "SizeGripStyle", "SortOrder", "SpecialFolderEnumConverter", "SplitContainer", "Splitter", "SplitterCancelEventArgs", "SplitterCancelEventHandler", "SplitterEventArgs", "SplitterEventHandler", "SplitterPanel", "StatusBar", "StatusBarDrawItemEventArgs", "StatusBarDrawItemEventHandler", "StatusBarPanel", "StatusBarPanelAutoSize", "StatusBarPanelBorderStyle", "StatusBarPanelClickEventArgs", "StatusBarPanelClickEventHandler", "StatusBarPanelStyle", "StatusStrip", "StringSorter", "StringSource", "StructFormat", "SystemInformation", "SystemParameter", "TabAlignment", "TabAppearance", "TabControl", "TabControlAction", "TabControlCancelEventArgs", "TabControlCancelEventHandler", "TabControlEventArgs", "TabControlEventHandler", "TabDrawMode", "TableLayoutPanel", "TableLayoutControlCollection", "TableLayoutPanelCellBorderStyle", "TableLayoutPanelCellPosition", "TableLayoutPanelCellPositionTypeConverter", "TableLayoutPanelGrowStyle", "TableLayoutSettings", "SizeType", "ColumnStyle", "RowStyle", "TableLayoutStyle", "TableLayoutStyleCollection", "TableLayoutCellPaintEventArgs", "TableLayoutCellPaintEventHandler", "TableLayoutColumnStyleCollection", "TableLayoutRowStyleCollection", "TabPage", "TabRenderer", "TabSizeMode", "TextBox", "TextBoxAutoCompleteSourceConverter", "TextBoxBase", "TextBoxRenderer", "TextDataFormat", "TextImageRelation", "ThreadExceptionDialog", "TickStyle", "ToolBar", "ToolBarAppearance", "ToolBarButton", "ToolBarButtonClickEventArgs", "ToolBarButtonClickEventHandler", "ToolBarButtonStyle", "ToolBarTextAlign", "ToolStrip", "CachedItemHdcInfo", "MouseHoverTimer", "ToolStripSplitStackDragDropHandler", "ToolStripArrowRenderEventArgs", "ToolStripArrowRenderEventHandler", "ToolStripButton", "ToolStripComboBox", "ToolStripControlHost", "ToolStripDropDown", "ToolStripDropDownCloseReason", "ToolStripDropDownClosedEventArgs", "ToolStripDropDownClosedEventHandler", "ToolStripDropDownClosingEventArgs", "ToolStripDropDownClosingEventHandler", "ToolStripDropDownDirection", "ToolStripDropDownButton", "ToolStripDropDownItem", "ToolStripDropDownItemAccessibleObject", "ToolStripDropDownMenu", "ToolStripDropTargetManager", "ToolStripHighContrastRenderer", "ToolStripGrip", "ToolStripGripDisplayStyle", "ToolStripGripRenderEventArgs", "ToolStripGripRenderEventHandler", "ToolStripGripStyle", "ToolStripItem", "ToolStripItemImageIndexer", "ToolStripItemInternalLayout", "ToolStripItemAlignment", "ToolStripItemClickedEventArgs", "ToolStripItemClickedEventHandler", "ToolStripItemCollection", "ToolStripItemDisplayStyle", "ToolStripItemEventArgs", "ToolStripItemEventHandler", "ToolStripItemEventType", "ToolStripItemImageRenderEventArgs", "ToolStripItemImageRenderEventHandler", "ToolStripItemImageScaling", "ToolStripItemOverflow", "ToolStripItemPlacement", "ToolStripItemRenderEventArgs", "ToolStripItemRenderEventHandler", "ToolStripItemStates", "ToolStripItemTextRenderEventArgs", "ToolStripItemTextRenderEventHandler", "ToolStripLabel", "ToolStripLayoutStyle", "ToolStripManager", "ToolStripCustomIComparer", "MergeHistory", "MergeHistoryItem", "ToolStripManagerRenderMode", "ToolStripMenuItem", "MenuTimer", "ToolStripMenuItemInternalLayout", "ToolStripOverflow", "ToolStripOverflowButton", "ToolStripContainer", "ToolStripContentPanel", "ToolStripPanel", "ToolStripPanelCell", "ToolStripPanelRenderEventArgs", "ToolStripPanelRenderEventHandler", "ToolStripContentPanelRenderEventArgs", "ToolStripContentPanelRenderEventHandler", "ToolStripPanelRow", "ToolStripPointType", "ToolStripProfessionalRenderer", "ToolStripProfessionalLowResolutionRenderer", "ToolStripProgressBar", "ToolStripRenderer", "ToolStripRendererSwitcher", "ToolStripRenderEventArgs", "ToolStripRenderEventHandler", "ToolStripRenderMode", "ToolStripScrollButton", "ToolStripSeparator", "ToolStripSeparatorRenderEventArgs", "ToolStripSeparatorRenderEventHandler", "ToolStripSettings", "ToolStripSettingsManager", "ToolStripSplitButton", "ToolStripSplitStackLayout", "ToolStripStatusLabel", "ToolStripStatusLabelBorderSides", "ToolStripSystemRenderer", "ToolStripTextBox", "ToolStripTextDirection", "ToolStripLocationCancelEventArgs", "ToolStripLocationCancelEventHandler", "ToolTip", "ToolTipIcon", "TrackBar", "TrackBarRenderer", "TreeNode", "TreeNodeMouseClickEventArgs", "TreeNodeMouseClickEventHandler", "TreeNodeCollection", "TreeNodeConverter", "TreeNodeMouseHoverEventArgs", "TreeNodeMouseHoverEventHandler", "TreeNodeStates", "TreeView", "TreeViewAction", "TreeViewCancelEventArgs", "TreeViewCancelEventHandler", "TreeViewDrawMode", "TreeViewEventArgs", "TreeViewEventHandler", "TreeViewHitTestInfo", "TreeViewHitTestLocations", "TreeViewImageIndexConverter", "TreeViewImageKeyConverter", "Triangle", "TriangleDirection", "TypeValidationEventArgs", "TypeValidationEventHandler", "UICues", "UICuesEventArgs", "UICuesEventHandler", "UpDownBase", "UpDownEventArgs", "UpDownEventHandler", "UserControl", "ValidationConstraints", "View", "VScrollBar", "VScrollProperties", "WebBrowser", "WebBrowserEncryptionLevel", "WebBrowserReadyState", "WebBrowserRefreshOption", "WebBrowserBase", "WebBrowserContainer", "WebBrowserDocumentCompletedEventHandler", "WebBrowserDocumentCompletedEventArgs", "WebBrowserHelper", "WebBrowserNavigatedEventHandler", "WebBrowserNavigatedEventArgs", "WebBrowserNavigatingEventHandler", "WebBrowserNavigatingEventArgs", "WebBrowserProgressChangedEventHandler", "WebBrowserProgressChangedEventArgs", "WebBrowserSiteBase", "WebBrowserUriTypeConverter", "WinCategoryAttribute", "WindowsFormsSection", "WindowsFormsSynchronizationContext", "IntSecurity", "WindowsFormsUtils", "IComponentEditorPageSite", "LayoutSettings", "PageSetupDialog", "PrintControllerWithStatusDialog", "PrintDialog", "PrintPreviewControl", "PrintPreviewDialog", "TextFormatFlags", "TextRenderer", "WindowsGraphicsWrapper", "SRDescriptionAttribute", "SRCategoryAttribute", "SR", "VisualStyleElement", "VisualStyleInformation", "VisualStyleRenderer", "VisualStyleState", "ComboBoxState", "CheckBoxState", "GroupBoxState", "HeaderItemState", "PushButtonState", "RadioButtonState", "ScrollBarArrowButtonState", "ScrollBarState", "ScrollBarSizeBoxState", "TabItemState", "TextBoxState", "ToolBarState", "TrackBarThumbState", "BackgroundType", "BorderType", "ImageOrientation", "SizingType", "FillType", "HorizontalAlign", "ContentAlignment", "VerticalAlignment", "OffsetType", "IconEffect", "TextShadowType", "GlyphType", "ImageSelectType", "TrueSizeScalingType", "GlyphFontSizingType", "ColorProperty", "EnumProperty", "FilenameProperty", "FontProperty", "IntegerProperty", "PointProperty", "MarginProperty", "StringProperty", "BooleanProperty", "Edges", "EdgeStyle", "EdgeEffects", "TextMetrics", "TextMetricsPitchAndFamilyValues", "TextMetricsCharacterSet", "HitTestOptions", "HitTestCode", "ThemeSizeType", "VisualStyleDocProperty", "VisualStyleSystemProperty", "ArrayElementGridEntry", "CategoryGridEntry", "DocComment", "DropDownButton", "DropDownButtonAdapter", "GridEntry", "AttributeTypeSorter", "GridEntryRecreateChildrenEventHandler", "GridEntryRecreateChildrenEventArgs", "GridEntryCollection", "GridErrorDlg", "GridToolTip", "HotCommands", "ImmutablePropertyDescriptorGridEntry", "IRootGridEntry", "MergePropertyDescriptor", "MultiPropertyDescriptorGridEntry", "MultiSelectRootGridEntry", "PropertiesTab", "PropertyDescriptorGridEntry", "PropertyGridCommands", "PropertyGridView", "SingleSelectRootGridEntry", "ComponentEditorForm", "ComponentEditorPage", "EventsTab", "IUIService", "IWindowsFormsEditorService", "PropertyTab", "ToolStripItemDesignerAvailability", "ToolStripItemDesignerAvailabilityAttribute", "WindowsFormsComponentEditor", "BaseCAMarshaler", "Com2AboutBoxPropertyDescriptor", "Com2ColorConverter", "Com2ComponentEditor", "Com2DataTypeToManagedDataTypeConverter", "Com2Enum", "Com2EnumConverter", "Com2ExtendedBrowsingHandler", "Com2ExtendedTypeConverter", "Com2FontConverter", "Com2ICategorizePropertiesHandler", "Com2IDispatchConverter", "Com2IManagedPerPropertyBrowsingHandler", "Com2IPerPropertyBrowsingHandler", "Com2IProvidePropertyBuilderHandler", "Com2IVsPerPropertyBrowsingHandler", "Com2PictureConverter", "Com2Properties", "Com2PropertyBuilderUITypeEditor", "Com2PropertyDescriptor", "GetAttributesEvent", "Com2EventHandler", "GetAttributesEventHandler", "GetNameItemEvent", "GetNameItemEventHandler", "DynamicMetaObjectProviderDebugView", "ExpressionTreeCallRewriter", "ICSharpInvokeOrInvokeMemberBinder", "ResetBindException", "RuntimeBinder", "RuntimeBinderController", "RuntimeBinderException", "RuntimeBinderInternalCompilerException", "SpecialNames", "SymbolTable", "RuntimeBinderExtensions", "NameManager", "Name", "NameTable", "OperatorKind", "PredefinedName", "PredefinedType", "TokenFacts", "TokenKind", "OutputContext", "UNSAFESTATES", "CheckedContext", "BindingFlag", "ExpressionBinder", "BinOpKind", "BinOpMask", "CandidateFunctionMember", "ConstValKind", "CONSTVAL", "ConstValFactory", "ConvKind", "CONVERTTYPE", "BetterType", "ListExtensions", "CConversions", "Operators", "UdConvInfo", "ArgInfos", "BodyType", "ConstCastResult", "AggCastResult", "UnaryOperatorSignatureFindResult", "UnaOpKind", "UnaOpMask", "OpSigFlags", "LiftFlags", "CheckLvalueKind", "BinOpFuncKind", "UnaOpFuncKind", "ExpressionKind", "ExpressionKindExtensions", "EXPRExtensions", "ExprFactory", "EXPRFLAG", "FileRecord", "FUNDTYPE", "GlobalSymbolContext", "InputFile", "LangCompiler", "MemLookFlags", "MemberLookup", "CMemberLookupResults", "mdToken", "CorAttributeTargets", "MethodKindEnum", "MethodTypeInferrer", "NameGenerator", "CNullable", "NullableCallLiftKind", "CONSTRESKIND", "LambdaParams", "TypeOrSimpleNameResolution", "InitializerKind", "ConstantStringConcatenation", "ForeachKind", "PREDEFATTR", "PREDEFMETH", "PREDEFPROP", "MethodRequiredEnum", "MethodCallingConventionEnum", "MethodSignatureEnum", "PredefinedMethodInfo", "PredefinedPropertyInfo", "PredefinedMembers", "ACCESSERROR", "CSemanticChecker", "SubstTypeFlags", "SubstContext", "CheckConstraintsFlags", "TypeBind", "UtilityTypeExtensions", "SymWithType", "MethPropWithType", "MethWithType", "PropWithType", "EventWithType", "FieldWithType", "MethPropWithInst", "MethWithInst", "AggregateDeclaration", "Declaration", "GlobalAttributeDeclaration", "ITypeOrNamespace", "AggregateSymbol", "AssemblyQualifiedNamespaceSymbol", "EventSymbol", "FieldSymbol", "IndexerSymbol", "LabelSymbol", "LocalVariableSymbol", "MethodOrPropertySymbol", "MethodSymbol", "InterfaceImplementationMethodSymbol", "IteratorFinallyMethodSymbol", "MiscSymFactory", "NamespaceOrAggregateSymbol", "NamespaceSymbol", "ParentSymbol", "PropertySymbol", "Scope", "KAID", "ACCESS", "AggKindEnum", "ARRAYMETHOD", "SpecCons", "Symbol", "SymbolExtensions", "SymFactory", "SymFactoryBase", "SYMKIND", "SynthAggKind", "SymbolLoader", "AidContainer", "BSYMMGR", "symbmask_t", "SYMTBL", "TransparentIdentifierMemberSymbol", "TypeParameterSymbol", "UnresolvedAggregateSymbol", "VariableSymbol", "EXPRARRAYINDEX", "EXPRARRINIT", "EXPRARRAYLENGTH", "EXPRASSIGNMENT", "EXPRBINOP", "EXPRBLOCK", "EXPRBOUNDLAMBDA", "EXPRCALL", "EXPRCAST", "EXPRCLASS", "EXPRMULTIGET", "EXPRMULTI", "EXPRCONCAT", "EXPRQUESTIONMARK", "EXPRCONSTANT", "EXPREVENT", "EXPR", "ExpressionIterator", "EXPRFIELD", "EXPRFIELDINFO", "EXPRHOISTEDLOCALEXPR", "EXPRLIST", "EXPRLOCAL", "EXPRMEMGRP", "EXPRMETHODINFO", "EXPRFUNCPTR", "EXPRNamedArgumentSpecification", "EXPRPROP", "EXPRPropertyInfo", "EXPRRETURN", "EXPRSTMT", "EXPRWRAP", "EXPRTHISPOINTER", "EXPRTYPEARGUMENTS", "EXPRTYPEOF", "EXPRTYPEORNAMESPACE", "EXPRUNARYOP", "EXPRUNBOUNDLAMBDA", "EXPRUSERDEFINEDCONVERSION", "EXPRUSERLOGOP", "EXPRZEROINIT", "ExpressionTreeRewriter", "ExprVisitorBase", "AggregateType", "ArgumentListType", "ArrayType", "BoundLambdaType", "ErrorType", "MethodGroupType", "NullableType", "NullType", "OpenTypePlaceholderType", "ParameterModifierType", "PointerType", "PredefinedTypes", "PredefinedTypeFacts", "CType", "TypeArray", "TypeFactory", "TypeManager", "TypeParameterType", "KeyPair`2", "TypeTable", "VoidType", "CError", "CParameterizedError", "CErrorFactory", "ErrorFacts", "ErrArgKind", "ErrArgFlags", "SymWithTypeMemo", "MethPropWithInstMemo", "ErrArg", "ErrArgRef", "ErrArgRefOnly", "ErrArgNoRef", "ErrArgIds", "ErrArgSymKind", "ErrorHandling", "IErrorSink", "MessageID", "UserStringBuilder", "CController", "<Cons>d__10`1", "<Cons>d__11`1", "DynamicProperty", "DynamicDebugViewEmptyException", "<>c__DisplayClass20_0", "ExpressionEXPR", "ArgumentObject", "NameHashKey", "<>c__DisplayClass18_0", "<>c__DisplayClass18_1", "<>c__DisplayClass43_0", "<>c__DisplayClass45_0", "KnownName", "BinOpArgInfo", "BinOpSig", "BinOpFullSig", "ConversionFunc", "ExplicitConversion", "PfnBindBinOp", "PfnBindUnaOp", "GroupToArgsBinder", "GroupToArgsBinderResult", "ImplicitConversion", "UnaOpSig", "UnaOpFullSig", "OPINFO", "<ToEnumerable>d__1", "CMethodIterator", "NewInferenceResult", "Dependency", "<InterfaceAndBases>d__0", "<AllConstraintInterfaces>d__1", "<TypeAndBaseClasses>d__2", "<TypeAndBaseClassInterfaces>d__3", "<AllPossibleInterfaces>d__4", "<Children>d__0", "Kind", "TypeArrayKey", "Key", "PredefinedTypeInfo", "StdTypeVarColl", "<>c__DisplayClass71_0", "__StaticArrayInitTypeSize=104", "__StaticArrayInitTypeSize=169", "SNINativeMethodWrapper", "QTypes", "ProviderEnum", "IOType", "ConsumerNumber", "SqlAsyncCallbackDelegate", "ConsumerInfo", "SNI_Error", "Win32NativeMethods", "NativeOledbWrapper", "AdalException", "ADALNativeWrapper", "Sni_Consumer_Info", "SNI_ConnWrapper", "SNI_Packet_IOType", "ConsumerNum", "$ArrayType$$$BY08$$CBG", "_GUID", "SNI_CLIENT_CONSUMER_INFO", "IUnknown", "__s_GUID", "IChapteredRowset", "_FILETIME", "ProviderNum", "ITransactionLocal", "SNI_ERROR", "$ArrayType$$$BY08G", "BOID", "ModuleLoadException", "ModuleLoadExceptionHandlerException", "ModuleUninitializer", "LanguageSupport", "gcroot<System::String ^>", "$ArrayType$$$BY00Q6MPBXXZ", "Progress", "$ArrayType$$$BY0A@P6AXXZ", "$ArrayType$$$BY0A@P6AHXZ", "__enative_startup_state", "TriBool", "ICLRRuntimeHost", "ThisModule", "_EXCEPTION_POINTERS", "Bid", "SqlDependencyProcessDispatcher", "BidIdentityAttribute", "BidMetaTextAttribute", "BidMethodAttribute", "BidArgumentTypeAttribute", "ExtendedClrTypeCode", "ITypedGetters", "ITypedGettersV3", "ITypedSetters", "ITypedSettersV3", "MetaDataUtilsSmi", "SmiConnection", "SmiContext", "SmiContextFactory", "SmiEventSink", "SmiEventSink_Default", "SmiEventSink_DeferedProcessing", "SmiEventStream", "SmiExecuteType", "SmiGettersStream", "SmiLink", "SmiMetaData", "SmiExtendedMetaData", "SmiParameterMetaData", "SmiStorageMetaData", "SmiQueryMetaData", "SmiRecordBuffer", "SmiRequestExecutor", "SmiSettersStream", "SmiStream", "SmiXetterAccessMap", "SmiXetterTypeCode", "SqlContext", "SqlDataRecord", "SqlPipe", "SqlTriggerContext", "ValueUtilsSmi", "SqlClientWrapperSmiStream", "SqlClientWrapperSmiStreamChars", "IBinarySerialize", "InvalidUdtException", "SqlFacetAttribute", "DataAccessKind", "SystemDataAccessKind", "SqlFunctionAttribute", "SqlMetaData", "SqlMethodAttribute", "FieldInfoEx", "BinaryOrderedUdtNormalizer", "Normalizer", "BooleanNormalizer", "SByteNormalizer", "ByteNormalizer", "ShortNormalizer", "UShortNormalizer", "IntNormalizer", "UIntNormalizer", "LongNormalizer", "ULongNormalizer", "FloatNormalizer", "DoubleNormalizer", "SqlProcedureAttribute", "SerializationHelperSql9", "Serializer", "NormalizedSerializer", "BinarySerializeSerializer", "DummyStream", "SqlTriggerAttribute", "SqlUserDefinedAggregateAttribute", "SqlUserDefinedTypeAttribute", "TriggerAction", "MemoryRecordBuffer", "SmiPropertySelector", "SmiMetaDataPropertyCollection", "SmiMetaDataProperty", "SmiUniqueKeyProperty", "SmiOrderProperty", "SmiDefaultFieldsProperty", "SmiTypedGetterSetter", "SqlRecordBuffer", "BaseTreeIterator", "DataDocumentXPathNavigator", "DataPointer", "DataSetMapper", "IXmlDataVirtualNode", "BaseRegionIterator", "RegionIterator", "TreeIterator", "ElementState", "XmlBoundElement", "XmlDataDocument", "XmlDataImplementation", "XPathNodePointer", "AcceptRejectRule", "InternalDataCollectionBase", "TypedDataSetGenerator", "StrongTypingException", "TypedDataSetGeneratorException", "ColumnTypeConverter", "CommandBehavior", "CommandType", "KeyRestrictionBehavior", "ConflictOption", "ConnectionState", "Constraint", "ConstraintCollection", "ConstraintConverter", "ConstraintEnumerator", "ForeignKeyConstraintEnumerator", "ChildForeignKeyConstraintEnumerator", "ParentForeignKeyConstraintEnumerator", "DataColumn", "AutoIncrementValue", "AutoIncrementInt64", "AutoIncrementBigInteger", "DataColumnChangeEventArgs", "DataColumnChangeEventHandler", "DataColumnCollection", "DataColumnPropertyDescriptor", "DataError", "DataException", "ConstraintException", "DeletedRowInaccessibleException", "DuplicateNameException", "InRowChangingEventException", "InvalidConstraintException", "MissingPrimaryKeyException", "NoNullAllowedException", "ReadOnlyException", "RowNotInTableException", "VersionNotFoundException", "ExceptionBuilder", "DataKey", "DataRelation", "DataRelationCollection", "DataRelationPropertyDescriptor", "DataRow", "DataRowBuilder", "DataRowAction", "DataRowChangeEventArgs", "DataRowChangeEventHandler", "DataRowCollection", "DataRowCreatedEventHandler", "DataSetClearEventhandler", "DataRowState", "DataRowVersion", "DataRowView", "SerializationFormat", "DataSet", "DataSetSchemaImporterExtension", "DataSetDateTime", "DataSysDescriptionAttribute", "DataTable", "DataTableClearEventArgs", "DataTableClearEventHandler", "DataTableCollection", "DataTableNewRowEventArgs", "DataTableNewRowEventHandler", "DataTablePropertyDescriptor", "DataTableReader", "DataTableReaderListener", "DataTableTypeConverter", "DataView", "DataViewListener", "DataViewManager", "DataViewManagerListItemTypeDescriptor", "DataViewRowState", "DataViewSetting", "DataViewSettingCollection", "DBConcurrencyException", "DbType", "DefaultValueTypeConverter", "FillErrorEventArgs", "FillErrorEventHandler", "AggregateNode", "BinaryNode", "LikeNode", "ConstNode", "DataExpression", "ExpressionNode", "ExpressionParser", "Tokens", "OperatorInfo", "InvalidExpressionException", "EvaluateException", "SyntaxErrorException", "ExprException", "FunctionNode", "FunctionId", "Function", "IFilter", "LookupNode", "NameNode", "UnaryNode", "ZeroOpNode", "ForeignKeyConstraint", "IColumnMapping", "IColumnMappingCollection", "IDataAdapter", "IDataParameter", "IDataParameterCollection", "IDataReader", "IDataRecord", "IDbCommand", "IDbConnection", "IDbDataAdapter", "IDbDataParameter", "IDbTransaction", "IsolationLevel", "ITableMapping", "ITableMappingCollection", "LoadOption", "MappingType", "MergeFailedEventArgs", "MergeFailedEventHandler", "Merger", "MissingMappingAction", "MissingSchemaAction", "OperationAbortedException", "ParameterDirection", "PrimaryKeyTypeConverter", "PropertyCollection", "RBTreeError", "TreeAccessMethod", "RBTree`1", "RecordManager", "StatementCompletedEventArgs", "StatementCompletedEventHandler", "RelatedView", "RelationshipConverter", "Rule", "SchemaSerializationMode", "SchemaType", "IndexField", "Index", "Listeners`1", "SimpleType", "LocalDBAPI", "LocalDBInstanceElement", "LocalDBInstancesCollection", "LocalDBConfigurationSection", "SqlDbType", "StateChangeEventArgs", "StateChangeEventHandler", "StatementType", "UniqueConstraint", "UpdateRowSource", "UpdateStatus", "XDRSchema", "XmlDataLoader", "XMLDiffLoader", "XmlReadMode", "SchemaFormat", "XmlTreeGen", "NewDiffgramGen", "XmlDataTreeWriter", "DataTextWriter", "DataTextReader", "XMLSchema", "ConstraintTable", "XSDSchema", "XmlIgnoreNamespaceReader", "XmlToDatasetMap", "XmlWriteMode", "SqlEventSource", "SqlDataSourceEnumerator", "SqlGenericUtil", "SqlNotificationRequest", "INullable", "SqlBinary", "SqlBoolean", "SqlByte", "SqlBytesCharsState", "SqlBytes", "StreamOnSqlBytes", "SqlChars", "StreamOnSqlChars", "SqlStreamChars", "SqlDateTime", "SqlDecimal", "SqlDouble", "SqlFileStream", "UnicodeString", "SecurityQualityOfService", "FileFullEaInformation", "SqlGuid", "SqlInt16", "SqlInt32", "SqlInt64", "SqlMoney", "SQLResource", "SqlSingle", "SqlCompareOptions", "SqlString", "SqlTypesSchemaImporterExtensionHelper", "TypeCharSchemaImporterExtension", "TypeNCharSchemaImporterExtension", "TypeVarCharSchemaImporterExtension", "TypeNVarCharSchemaImporterExtension", "TypeTextSchemaImporterExtension", "TypeNTextSchemaImporterExtension", "TypeVarBinarySchemaImporterExtension", "TypeBinarySchemaImporterExtension", "TypeVarImageSchemaImporterExtension", "TypeDecimalSchemaImporterExtension", "TypeNumericSchemaImporterExtension", "TypeBigIntSchemaImporterExtension", "TypeIntSchemaImporterExtension", "TypeSmallIntSchemaImporterExtension", "TypeTinyIntSchemaImporterExtension", "TypeBitSchemaImporterExtension", "TypeFloatSchemaImporterExtension", "TypeRealSchemaImporterExtension", "TypeDateTimeSchemaImporterExtension", "TypeSmallDateTimeSchemaImporterExtension", "TypeMoneySchemaImporterExtension", "TypeSmallMoneySchemaImporterExtension", "TypeUniqueIdentifierSchemaImporterExtension", "EComparison", "StorageState", "SqlTypeException", "SqlNullValueException", "SqlTruncateException", "SqlNotFilledException", "SqlAlreadyFilledException", "SQLDebug", "SqlXml", "SqlXmlStreamWrapper", "SqlClientEncryptionAlgorithmFactoryList", "SqlSymmetricKeyCache", "SqlColumnEncryptionKeyStoreProvider", "SqlColumnEncryptionCertificateStoreProvider", "SqlColumnEncryptionCngProvider", "SqlColumnEncryptionCspProvider", "SqlAeadAes256CbcHmac256Algorithm", "SqlAeadAes256CbcHmac256Factory", "SqlAeadAes256CbcHmac256EncryptionKey", "SqlAes256CbcAlgorithm", "SqlAes256CbcFactory", "SqlClientEncryptionAlgorithm", "SqlClientEncryptionAlgorithmFactory", "SqlClientEncryptionType", "SqlClientSymmetricKey", "SqlSecurityUtility", "SqlQueryMetadataCache", "ApplicationIntent", "SqlCredential", "SqlConnectionPoolKey", "AssemblyCache", "OnChangeEventHandler", "SqlRowsCopiedEventArgs", "SqlRowsCopiedEventHandler", "SqlBuffer", "_ColumnMapping", "Row", "BulkCopySimpleResultSet", "SqlBulkCopy", "SqlBulkCopyColumnMapping", "SqlBulkCopyColumnMappingCollection", "SqlBulkCopyOptions", "SqlCachedBuffer", "SqlClientFactory", "SqlClientMetaDataCollectionNames", "SqlClientPermission", "SqlClientPermissionAttribute", "SqlCommand", "SqlCommandBuilder", "SqlCommandSet", "SqlConnection", "SQLDebugging", "ISQLDebug", "SqlDebugContext", "MEMMAP", "SqlConnectionFactory", "SqlPerformanceCounters", "SqlConnectionPoolGroupProviderInfo", "SqlConnectionPoolProviderInfo", "SqlConnectionString", "SqlConnectionStringBuilder", "SqlConnectionTimeoutErrorPhase", "SqlConnectionInternalSourceType", "SqlConnectionTimeoutPhaseDuration", "SqlConnectionTimeoutErrorInternal", "SqlDataAdapter", "SqlDataReader", "SqlDataReaderSmi", "SqlDelegatedTransaction", "SqlDependency", "SqlDependencyPerAppDomainDispatcher", "SqlNotification", "MetaType", "TdsDateTime", "SqlError", "SqlErrorCollection", "SqlException", "SqlInfoMessageEventArgs", "SqlInfoMessageEventHandler", "SqlInternalConnection", "SqlInternalConnectionSmi", "SessionStateRecord", "SessionData", "SqlInternalConnectionTds", "ServerInfo", "TransactionState", "TransactionType", "SqlInternalTransaction", "SqlMetaDataFactory", "SqlNotificationEventArgs", "SqlNotificationInfo", "SqlNotificationSource", "SqlNotificationType", "DataFeed", "StreamDataFeed", "TextDataFeed", "XmlDataFeed", "SqlParameter", "SqlParameterCollection", "SqlReferenceCollection", "SqlRowUpdatedEventArgs", "SqlRowUpdatedEventHandler", "SqlRowUpdatingEventArgs", "SqlRowUpdatingEventHandler", "SqlSequentialStream", "SqlSequentialStreamSmi", "System.Diagnostics.DebuggableAttribute", "System.Diagnostics", "System.Net.WebClient", "System", "System.Specialized.Protection" };
            return charsc[rnd.Next(charsc.Length)];
        }
        private static void FloorReplacer(MethodDef method, Instruction instruction, ref int i)
        {
            try
            {
                if (instruction.Operand != null)
                    if (instruction.IsLdcI4())
                    {
                        if (instruction.GetLdcI4Value() < int.MaxValue)
                        {
                            int orig = (int)instruction.Operand;
                            double m = (double)orig + RandomDouble(0.01, 0.99);
                            instruction.OpCode = OpCodes.Ldc_R8;
                            instruction.Operand = m;
                            method.Body.Instructions.Insert(i + 1, OpCodes.Call.ToInstruction(method.Module.Import(typeof(Math).GetMethod("Floor", new Type[] { typeof(double) }))));
                            method.Body.Instructions.Insert(i + 2, OpCodes.Conv_I4.ToInstruction());
                        }
                    }
            }
            catch { }
        }
        public static void IfInliner(MethodDef method)
        {
            Local local = new Local(method.Module.ImportAsTypeSig(typeof(int)));
            method.Body.Variables.Add(local);
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                if (method.Body.Instructions[i].IsLdcI4())
                {
                    if (CanObfuscateLDCI4(method.Body.Instructions, i))
                    {
                        int numorig = rnd.Next();
                        int div = rnd.Next();
                        int num = numorig ^ div;

                        Instruction nop = OpCodes.Nop.ToInstruction();
                        method.Body.Instructions.Insert(i + 1, OpCodes.Stloc_S.ToInstruction(local));
                        method.Body.Instructions.Insert(i + 2, Instruction.Create(OpCodes.Ldc_I4, method.Body.Instructions[i].GetLdcI4Value() - sizeof(float)));
                        method.Body.Instructions.Insert(i + 3, Instruction.Create(OpCodes.Ldc_I4, num));
                        method.Body.Instructions.Insert(i + 4, Instruction.Create(OpCodes.Ldc_I4, div));
                        method.Body.Instructions.Insert(i + 5, Instruction.Create(OpCodes.Xor));
                        method.Body.Instructions.Insert(i + 6, Instruction.Create(OpCodes.Ldc_I4, numorig));
                        method.Body.Instructions.Insert(i + 7, Instruction.Create(OpCodes.Bne_Un, nop));
                        method.Body.Instructions.Insert(i + 8, Instruction.Create(OpCodes.Ldc_I4, 2));
                        method.Body.Instructions.Insert(i + 9, OpCodes.Stloc_S.ToInstruction(local));
                        method.Body.Instructions.Insert(i + 10, Instruction.Create(OpCodes.Sizeof, method.Module.Import(typeof(float))));
                        method.Body.Instructions.Insert(i + 11, Instruction.Create(OpCodes.Add));
                        method.Body.Instructions.Insert(i + 12, nop);
                        i += 12;
                    }
                }
            }
        }
        public static void InlineInteger(MethodDef method)
        {
            Local new_local = new Local(method.Module.CorLibTypes.String);
            method.Body.Variables.Add(new_local);
            Local new_local2 = new Local(method.Module.CorLibTypes.Int32);
            method.Body.Variables.Add(new_local2);
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                if (method.Body.Instructions[i].IsLdcI4())
                {
                    if (CanObfuscateLDCI4(method.Body.Instructions, i))
                    {
                        if (method.DeclaringType.IsGlobalModuleType) return;
                        if (!method.HasBody) return;
                        var instr = method.Body.Instructions;
                        if ((i - 1) > 0)
                            try
                            {

                                if (instr[i - 1].OpCode == OpCodes.Callvirt)
                                {
                                    if (instr[i + 1].OpCode == OpCodes.Call)
                                    {
                                        return;
                                    }
                                }
                            }
                            catch { }
                        bool is_valid_inline = true;
                        switch (rnd.Next(0, 2))
                        {
                            case 0:
                                is_valid_inline = true;
                                break;
                            case 1:
                                is_valid_inline = false;
                                break;
                        }


                        var value = instr[i].GetLdcI4Value();
                        var first_ldstr = RandomString(5, "畹畞疲疷疹痲痹痹瘕番畐畞畵畵畲畲蘽蘐藴虜蘞虢謊謁");

                        instr.Insert(i, Instruction.Create(OpCodes.Ldloc_S, new_local2));

                        instr.Insert(i, Instruction.Create(OpCodes.Stloc_S, new_local2));
                        if (is_valid_inline)
                        {
                            instr.Insert(i, Instruction.Create(OpCodes.Ldc_I4, value));
                            instr.Insert(i, Instruction.Create(OpCodes.Ldc_I4, value + 1));
                        }
                        else
                        {
                            instr.Insert(i, Instruction.Create(OpCodes.Ldc_I4, value + 1));
                            instr.Insert(i, Instruction.Create(OpCodes.Ldc_I4, value));
                        }
                        instr.Insert(i,
                            Instruction.Create(OpCodes.Call,
                                method.Module.Import(typeof(System.String).GetMethod("op_Equality",
                                    new Type[] { typeof(string), typeof(string) }))));
                        instr.Insert(i, Instruction.Create(OpCodes.Ldstr, first_ldstr));
                        instr.Insert(i, Instruction.Create(OpCodes.Ldloc_S, new_local));
                        instr.Insert(i, Instruction.Create(OpCodes.Stloc_S, new_local));
                        if (is_valid_inline)
                        {
                            instr.Insert(i, Instruction.Create(OpCodes.Ldstr, first_ldstr));
                        }
                        else
                        {
                            instr.Insert(i,
                                Instruction.Create(OpCodes.Ldstr,
                                   RandomString(7, "畹畞疲疷疹痲痹痹瘕番畐畞畵畵畲畲蘽蘐藴虜蘞虢謊謁")));
                        }
                        instr.Insert(i + 5, Instruction.Create(OpCodes.Brtrue_S, instr[i + 6]));
                        instr.Insert(i + 7, Instruction.Create(OpCodes.Br_S, instr[i + 8]));
                        instr.RemoveAt(i + 10);
                        i += 10;
                    }
                }
            }
        }
        private static void RoundReplacer(MethodDef method, Instruction instruction, ref int i)
        {
            try
            {
                if (instruction.Operand != null)
                    if (instruction.IsLdcI4())
                    {
                        if (instruction.GetLdcI4Value() < int.MaxValue)
                        {
                            int orig = (int)instruction.Operand;
                            double m = (double)orig + RandomDouble(0.01, 0.5);
                            instruction.OpCode = OpCodes.Ldc_R8;
                            instruction.Operand = m;
                            method.Body.Instructions.Insert(i + 1, OpCodes.Call.ToInstruction(method.Module.Import(typeof(Math).GetMethod("Round", new Type[] { typeof(double) }))));
                            method.Body.Instructions.Insert(i + 2, OpCodes.Conv_I4.ToInstruction());
                        }
                    }
            }
            catch { }
        }
        private static void SqrtReplacer(MethodDef method, Instruction instruction, ref int i)
        {
            try
            {
                if (instruction.Operand != null)
                    if (instruction.IsLdcI4())
                    {
                        if (instruction.GetLdcI4Value() < int.MaxValue)
                        {
                            if ((int)instruction.Operand > 1)
                            {
                                int orig = (int)instruction.Operand;
                                double m = (double)orig * orig;
                                instruction.OpCode = OpCodes.Ldc_R8;
                                instruction.Operand = m;
                                method.Body.Instructions.Insert(i + 1, OpCodes.Call.ToInstruction(method.Module.Import(typeof(Math).GetMethod("Sqrt", new Type[] { typeof(double) }))));
                                method.Body.Instructions.Insert(i + 2, OpCodes.Conv_I4.ToInstruction());
                            }
                        }
                    }
            }
            catch { }
        }
        private static void CeilingReplacer(MethodDef method, Instruction instruction, ref int i)
        {
            try
            {
                if (instruction.Operand != null)
                    if (instruction.IsLdcI4())
                    {
                        if (instruction.GetLdcI4Value() < int.MaxValue)
                        {
                            int orig = (int)instruction.Operand;
                            double m = (double)orig - 1 + RandomDouble(0.01, 0.99);
                            instruction.OpCode = OpCodes.Ldc_R8;
                            instruction.Operand = m;
                            method.Body.Instructions.Insert(i + 1, OpCodes.Call.ToInstruction(method.Module.Import(typeof(Math).GetMethod("Ceiling", new Type[] { typeof(double) }))));
                            method.Body.Instructions.Insert(i + 2, OpCodes.Conv_I4.ToInstruction());
                        }
                    }
            }
            catch { }
        }
