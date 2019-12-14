/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Il2CppInspector.Reflection {
    public class TypeInfo : MemberInfo
    {
        // IL2CPP-specific data
        public Il2CppTypeDefinition Definition { get; }
        public int Index { get; } = -1;

        // Information/flags about the type
        // Undefined if the Type represents a generic type parameter
        public TypeAttributes Attributes { get; }

        // Type that this type inherits from
        private readonly int baseTypeUsage = -1;

        public TypeInfo BaseType => IsPointer? null :
            baseTypeUsage != -1?
                Assembly.Model.GetTypeFromUsage(baseTypeUsage, MemberTypes.TypeInfo)
                : IsArray? Assembly.Model.TypesByFullName["System.Array"]
                : Namespace != "System" || BaseName != "Object" ? Assembly.Model.TypesByFullName["System.Object"]
                : null;

        // True if the type contains unresolved generic type parameters
        public bool ContainsGenericParameters { get; }

        public string BaseName => base.Name;

        // Get rid of generic backticks
        public string UnmangledBaseName => base.Name.IndexOf("`", StringComparison.Ordinal) == -1 ? base.Name : base.Name.Remove(base.Name.IndexOf("`", StringComparison.Ordinal));

        // C# colloquial name of the type (if available)
        public string CSharpName {
            get {
                var s = Namespace + "." + base.Name;
                var i = Il2CppConstants.FullNameTypeString.IndexOf(s);
                var n = (i != -1 ? Il2CppConstants.CSharpTypeString[i] : base.Name);
                if (n?.IndexOf("`", StringComparison.Ordinal) != -1)
                    n = n?.Remove(n.IndexOf("`", StringComparison.Ordinal));
                var g = (GenericTypeParameters != null ? "<" + string.Join(", ", GenericTypeParameters.Select(x => x.CSharpName)) + ">" : "");
                g = (GenericTypeArguments != null ? "<" + string.Join(", ", GenericTypeArguments.Select(x => x.CSharpName)) + ">" : g);
                n += g;
                if (s == "System.Nullable`1" && GenericTypeArguments.Any())
                    n = GenericTypeArguments[0].CSharpName + "?";
                if (HasElementType)
                    n = ElementType.CSharpName;
                if ((GenericParameterAttributes & GenericParameterAttributes.Covariant) == GenericParameterAttributes.Covariant)
                    n = "out " + n;
                if ((GenericParameterAttributes & GenericParameterAttributes.Contravariant) == GenericParameterAttributes.Contravariant)
                    n = "in " + n;
                if (IsByRef)
                    n = "ref " + n;
                return n + (IsArray ? "[" + new string(',', GetArrayRank() - 1) + "]" : "") + (IsPointer ? "*" : "");
            }
        }

        // C# name as it would be written in a type declaration
        public string CSharpTypeDeclarationName {
            get {
                var gtp = IsNested? GenericTypeParameters?.Where(p => DeclaringType.GenericTypeParameters?.All(dp => dp.Name != p.Name) ?? true) : GenericTypeParameters;

                return (IsByRef ? "ref " : "")
                    + (HasElementType
                        ? ElementType.CSharpTypeDeclarationName
                        : ((GenericParameterAttributes & GenericParameterAttributes.Contravariant) == GenericParameterAttributes.Contravariant ? "in " : "")
                          + ((GenericParameterAttributes & GenericParameterAttributes.Covariant) == GenericParameterAttributes.Covariant ? "out " : "")
                          + (base.Name.IndexOf("`", StringComparison.Ordinal) == -1 ? base.Name : base.Name.Remove(base.Name.IndexOf("`", StringComparison.Ordinal)))
                          + (gtp?.Any() ?? false? "<" + string.Join(", ", gtp.Select(x => x.CSharpTypeDeclarationName)) + ">" : "")
                          + (GenericTypeArguments != null ? "<" + string.Join(", ", GenericTypeArguments.Select(x => (!x.IsGenericTypeParameter ? x.Namespace + "." : "") + x.CSharpTypeDeclarationName)) + ">" : ""))
                    + (IsArray ? "[" + new string(',', GetArrayRank() - 1) + "]" : "")
                    + (IsPointer ? "*" : "");
            }
        }

        // Custom attributes for this member
        public override IEnumerable<CustomAttributeData> CustomAttributes => CustomAttributeData.GetCustomAttributes(this);

        public List<ConstructorInfo> DeclaredConstructors { get; } = new List<ConstructorInfo>();
        public List<EventInfo> DeclaredEvents { get; } = new List<EventInfo>();
        public List<FieldInfo> DeclaredFields { get; } = new List<FieldInfo>();

        public List<MemberInfo> DeclaredMembers => new IEnumerable<MemberInfo>[] {
            DeclaredConstructors, DeclaredEvents, DeclaredFields, DeclaredMethods,
            DeclaredNestedTypes?.ToList() ?? new List<TypeInfo>(), DeclaredProperties
        }.SelectMany(m => m).ToList();

        public List<MethodInfo> DeclaredMethods { get; } = new List<MethodInfo>();

        private readonly int[] declaredNestedTypes;
        public IEnumerable<TypeInfo> DeclaredNestedTypes => declaredNestedTypes.Select(x => Assembly.Model.TypesByDefinitionIndex[x]);

        public List<PropertyInfo> DeclaredProperties { get; } = new List<PropertyInfo>();

        // Get a field by its name
        public FieldInfo GetField(string name) => DeclaredFields.FirstOrDefault(f => f.Name == name);

        private readonly int genericConstraintIndex;

        private readonly int genericConstraintCount;

        // Get type constraints on a generic parameter
        public TypeInfo[] GetGenericParameterConstraints() {
            var types = new TypeInfo[genericConstraintCount];
            for (int c = 0; c < genericConstraintCount; c++)
                types[c] = Assembly.Model.GetTypeFromUsage(Assembly.Model.Package.GenericConstraintIndices[genericConstraintIndex + c], MemberTypes.TypeInfo);
            return types;
        }

        // Get a method by its name
        public MethodInfo GetMethod(string name) => DeclaredMethods.FirstOrDefault(m => m.Name == name);

        // Get all methods with same name (overloads)
        public MethodInfo[] GetMethods(string name) => DeclaredMethods.Where(m => m.Name == name).ToArray();

        // Get methods including inherited methods
        public MethodInfo[] GetAllMethods() {
            var methods = new List<IEnumerable<MethodInfo>>();

            // Specifically return a list in order of most derived to least derived
            for (var type = this; type != null; type = type.BaseType)
                methods.Add(type.DeclaredMethods);

            return methods.SelectMany(m => m).ToArray();
        }

        // Get a property by its name
        public PropertyInfo GetProperty(string name) => DeclaredProperties.FirstOrDefault(p => p.Name == name);

        // Method that the type is declared in if this is a type parameter of a generic method
        // TODO: Make a unit test from this: https://docs.microsoft.com/en-us/dotnet/api/system.type.declaringmethod?view=netframework-4.8
        public MethodBase DeclaringMethod;
        
        // IsGenericTypeParameter and IsGenericMethodParameter from https://github.com/dotnet/corefx/issues/23883
        public bool IsGenericTypeParameter => IsGenericParameter && DeclaringMethod == null;
        public bool IsGenericMethodParameter => IsGenericParameter && DeclaringMethod != null;

        // Gets the type of the object encompassed or referred to by the current array, pointer or reference type
        public TypeInfo ElementType { get; }

        // Type name including namespace
        public string FullName =>
            IsGenericParameter? null :
            HasElementType && ElementType.IsGenericParameter? null :
                (HasElementType? ElementType.FullName : 
                    (DeclaringType != null? DeclaringType.FullName + "+" : Namespace + (Namespace.Length > 0? "." : ""))
                    + base.Name)
                + (IsArray? "[" + new string(',', GetArrayRank() - 1) + "]" : "")
                + (IsByRef? "&" : "")
                + (IsPointer? "*" : "");

        // Returns the minimally qualified type name required to refer to this type within the specified scope
        private string getScopedFullName(Scope scope) {
            // This is the type to be used (generic type parameters have a null FullName)
            var usedType = FullName?.Replace('+', '.') ?? Name;

            // This is the scope in which this type is currently being used
            // If Scope.Current is null, our scope is at the assembly level
            var usingScope = scope.Current?.FullName.Replace('+', '.') ?? "";

            // This is the scope in which this type's definition is located
            var declaringScope = DeclaringType?.FullName.Replace('+', '.') ?? Namespace;

            // Are we in the same scope as the scope the type is defined in? Save ourselves a bunch of work if so
            if (usingScope == declaringScope)
                return base.Name;

            // We're also in the same scope the type is defined in if we're looking for a nested type
            // that is declared in a type we derive from
            for (var b = scope.Current?.BaseType; b != null; b = b.BaseType)
                if (b.FullName.Replace('+', '.') == declaringScope)
                    return base.Name;

            // Find first difference in the declaring scope from the using scope, moving one namespace/type name at a time
            var diff = 1;
            usingScope += ".";
            declaringScope += ".";
            while (usingScope.IndexOf('.', diff) == declaringScope.IndexOf('.', diff)
                   && usingScope.IndexOf('.', diff) != -1
                   && usingScope.Substring(0, usingScope.IndexOf('.', diff))
                   == declaringScope.Substring(0, declaringScope.IndexOf('.', diff)))
                diff = usingScope.IndexOf('.', diff) + 1;
            usingScope = usingScope.Remove(usingScope.Length - 1);
            declaringScope = declaringScope.Remove(declaringScope.Length - 1);

            // This is the mutual root namespace and optionally nested types that the two scopes share
            var mutualRootScope = usingScope.Substring(0, diff - 1);

            // Determine if the using scope is a child of the declaring scope (always a child if declaring scope is empty)
            var usingScopeIsChildOfDeclaringScope = string.IsNullOrEmpty(declaringScope) || (usingScope + ".").StartsWith(declaringScope + ".");

            // Determine using directive to use
            var usingDirective =
                
                // If the scope of usage is inside the scope in which the type is declared, no additional scope is needed
                // but we still need to check for ancestor conflicts below
                usingScopeIsChildOfDeclaringScope? declaringScope
                
                // Check to see if there is a namespace in our using directives which brings this type into scope
                // Sort by descending order of length to search the deepest namespaces first
                : scope.Namespaces.OrderByDescending(n => n.Length).FirstOrDefault(n => declaringScope == n || declaringScope.StartsWith(n + "."));

            // minimallyScopedName will eventually contain the least qualified name needed to access the type
            // Initially we set it as follows:
            // - The non-mutual part of the declaring scope if there is a mutual root scope
            // - The fully-qualified type name if there is no mutual root scope
            // - The leaf name if the declaring scope and mutual root scope are the same
            // The first two must be checked in this order to avoid a . at the start
            // when the mutual root scope and declaring scope are both empty
            var minimallyScopedName =
                    declaringScope == mutualRootScope? base.Name :
                    string.IsNullOrEmpty(mutualRootScope)? declaringScope + '.' + base.Name :
                    declaringScope.Substring(mutualRootScope.Length + 1) + '.' + base.Name;

            // Find the outermost type name if the wanted type is a nested type (if we need it below)
            string outerTypeName = "";
            if (!usingScopeIsChildOfDeclaringScope)
                for (var d = this; d != null; d = d.DeclaringType)
                    outerTypeName = d.BaseName;

            // Are there any ancestor nested types or namespaces in the using scope with the same name as the wanted type's unqualified name?
            // If so, the ancestor name will hide the type we are trying to reference, so we need to provide a higher-level scope

            // If the using scope is a child of the declaring scope, we can try every parent scope until we find one that doesn't hide the type
            // Otherwise, we just try the unqualified outer (least nested) type name to make sure it's accessible
            // and revert to the fully qualified name if it's hidden
            var nsAndTypeHierarchy = usingScopeIsChildOfDeclaringScope? 
                usingDirective.Split('.').Append(minimallyScopedName).ToArray()
                : new [] {outerTypeName};

            var hidden = true;
            var foundTypeInAncestorScope = false;
            string testTypeName = "";

            for (var depth = nsAndTypeHierarchy.Length - 1; depth >= 0 && hidden; depth--) {
                testTypeName = nsAndTypeHierarchy[depth] + (testTypeName.Length > 0? "." : "") + testTypeName;

                hidden = false;
                for (var d = scope.Current; d != null && !hidden && !foundTypeInAncestorScope; d = d.DeclaringType) {
                    // If neither condition is true, the wanted type is not hidden by the type we are testing
                    foundTypeInAncestorScope = d.FullName == FullName;
                    hidden = !foundTypeInAncestorScope && d.BaseName == testTypeName;
                }

                // We found the shortest non-hidden scope we can use
                // For a child scope, use the shortest found scope
                // Otherwise, we've confirmed the outer nested type name is not hidden so go ahead and use the nested type name without a namespace
                if (!hidden)
                    minimallyScopedName = usingScopeIsChildOfDeclaringScope? testTypeName : Name.Replace('+', '.');

                // If the wanted type is an unhidden ancestor, we don't need any additional scope at all
                if (foundTypeInAncestorScope)
                    minimallyScopedName = base.Name;
            }

            // If there are multiple using directives that would allow the same minimally scoped name to be used,
            // then the minimally scoped name is ambiguous and we can't use it
            // Note that if the wanted type is an unhidden outer class relative to the using scope, this takes precedence and there can be no ambiguity
            if (!foundTypeInAncestorScope) {
                // Only test the outermost type name
                outerTypeName = minimallyScopedName.Split('.')[0];

                // Take matching type names from all namespaces in scope
                var matchingNamespaces = scope.Namespaces.Where(n => Assembly.Model.TypesByFullName.ContainsKey(n + "." + outerTypeName)).ToList();

                // The global namespace is in scope so take every matching type from that too
                if (Assembly.Model.TypesByFullName.ContainsKey(outerTypeName))
                    matchingNamespaces.Add("");

                // More than one possible matching type? If so, the type reference is ambiguous
                if (matchingNamespaces.Count > 1) {
                    // TODO: This can be improved to cut off a new mutual root that doesn't cause ambiguity
                    minimallyScopedName = usedType;
                }
            }
            return minimallyScopedName;
        }

        // C#-friendly type name as it should be used in the scope of a given type
        public string GetScopedCSharpName(Scope usingScope = null, bool omitRef = false) {
            // Unscoped name if no using scope specified
            if (usingScope == null)
                return CSharpName;

            var s = Namespace + "." + base.Name;

            // Built-in keyword type names do not require a scope
            var i = Il2CppConstants.FullNameTypeString.IndexOf(s);
            var n = i != -1 ? Il2CppConstants.CSharpTypeString[i] : getScopedFullName(usingScope);

            // Unmangle generic type names
            if (n?.IndexOf("`", StringComparison.Ordinal) != -1)
                n = n?.Remove(n.IndexOf("`", StringComparison.Ordinal));

            // Generic type parameters and type arguments
            var g = string.Join(", ", getGenericTypeParameters(usingScope).Select(x => x.GetScopedCSharpName(usingScope)));
            if (!string.IsNullOrEmpty(g))
                n += "<" + g + ">";

            // Nullable types
            if (s == "System.Nullable`1" && GenericTypeArguments.Any())
                n = GenericTypeArguments[0].GetScopedCSharpName(usingScope) + "?";

            // Arrays, pointers, references
            if (HasElementType)
                n = ElementType.GetScopedCSharpName(usingScope);

            return (IsByRef && !omitRef? "ref " : "") + n + (IsArray ? "[" + new string(',', GetArrayRank() - 1) + "]" : "") + (IsPointer ? "*" : "");
        }

        // Get the generic type parameters for a specific usage of this type based on its scope,
        // or all generic type parameters if no scope specified
        private IEnumerable<TypeInfo> getGenericTypeParameters(Scope scope = null) {
            // Merge generic type parameters and generic type arguments
            var gp = (GenericTypeParameters ?? new List<TypeInfo>()).Concat(GenericTypeArguments ?? new List<TypeInfo>());

            // If no scope or empty scope specified, or no type parameters, stop here
            if (scope?.Current == null || !gp.Any())
                return gp;

            // In order to elide generic type parameters, the using scope must be a parent of the declaring scope
            // Determine if the using scope is a parent of the declaring scope (always a child if using scope is empty)
            var usingScopeIsParent = false;
            for (var s = DeclaringType; s != null && !usingScopeIsParent; s = s.DeclaringType)
                if (s == scope.Current)
                    usingScopeIsParent = true;

            if (!usingScopeIsParent)
                return gp;

            // Get the generic type parameters available in the using scope
            // (no need to recurse because every nested type inherits all of the generic type parameters of all of its ancestors)
            var gpsInScope = (scope.Current.GenericTypeParameters ?? new List<TypeInfo>()).Concat(scope.Current.GenericTypeArguments ?? new List<TypeInfo>());

            // Return all of the generic type parameters this type uses minus those already in scope
            return gp.Where(p => gpsInScope.All(pp => pp.Name != p.Name));
        }

        public GenericParameterAttributes GenericParameterAttributes { get; }

        public int GenericParameterPosition { get; }

        public List<TypeInfo> GenericTypeParameters { get; }

        public List<TypeInfo> GenericTypeArguments { get; }

        // True if an array, pointer or reference, otherwise false
        // See: https://docs.microsoft.com/en-us/dotnet/api/system.type.haselementtype?view=netframework-4.8
        public bool HasElementType => ElementType != null;

        private readonly int[] implementedInterfaceUsages;
        public IEnumerable<TypeInfo> ImplementedInterfaces => implementedInterfaceUsages.Select(x => Assembly.Model.GetTypeFromUsage(x, MemberTypes.TypeInfo));

        public bool IsAbstract => (Attributes & TypeAttributes.Abstract) == TypeAttributes.Abstract;
        public bool IsArray { get; }
        public bool IsByRef { get; }
        public bool IsClass => (Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class;
        public bool IsEnum => enumUnderlyingTypeUsage != -1;
        public bool IsGenericParameter { get; }
        public bool IsGenericType { get; }
        public bool IsGenericTypeDefinition { get; }
        public bool IsImport => (Attributes & TypeAttributes.Import) == TypeAttributes.Import;
        public bool IsInterface => (Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface;
        public bool IsNested => (MemberType & MemberTypes.NestedType) == MemberTypes.NestedType;
        public bool IsNestedAssembly => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedAssembly;
        public bool IsNestedFamANDAssem => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamANDAssem;
        public bool IsNestedFamily => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily;
        public bool IsNestedFamORAssem => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamORAssem;
        public bool IsNestedPrivate => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate;
        public bool IsNestedPublic => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic;
        public bool IsNotPublic => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic;
        public bool IsPointer { get; }
        // Primitive types table: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/built-in-types-table (we exclude Object and String)
        public bool IsPrimitive => Namespace == "System" && new[] { "Boolean", "Byte", "SByte", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64", "IntPtr", "UIntPtr", "Char", "Decimal", "Double", "Single" }.Contains(Name);
        public bool IsPublic => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public;
        public bool IsSealed => (Attributes & TypeAttributes.Sealed) == TypeAttributes.Sealed;
        public bool IsSerializable => (Attributes & TypeAttributes.Serializable) == TypeAttributes.Serializable;
        public bool IsSpecialName => (Attributes & TypeAttributes.SpecialName) == TypeAttributes.SpecialName;
        public bool IsValueType => BaseType?.FullName == "System.ValueType";

        // Helper function for determining if using this type as a field, parameter etc. requires that field or method to be declared as unsafe
        public bool RequiresUnsafeContext => IsPointer || (HasElementType && ElementType.RequiresUnsafeContext);

        // May get overridden by Il2CppType-based constructor below
        public override MemberTypes MemberType { get; } = MemberTypes.TypeInfo;

        private string @namespace;
        public string Namespace {
            get => !string.IsNullOrEmpty(@namespace) ? @namespace : DeclaringType?.Namespace ?? "";
            set => @namespace = value;
        }

        // Number of dimensions of an array
        private readonly int arrayRank;
        public int GetArrayRank() => arrayRank;

        public string[] GetEnumNames() => IsEnum? DeclaredFields.Where(x => x.Name != "value__").Select(x => x.Name).ToArray() : throw new InvalidOperationException("Type is not an enumeration");

        // The underlying type of an enumeration (int by default)
        private readonly int enumUnderlyingTypeUsage = -1;
        private TypeInfo enumUnderlyingType;

        public TypeInfo GetEnumUnderlyingType() {
            if (!IsEnum)
                return null;
            enumUnderlyingType ??= Assembly.Model.GetTypeFromUsage(enumUnderlyingTypeUsage, MemberTypes.TypeInfo);
            return enumUnderlyingType;
        }

        public Array GetEnumValues() => IsEnum? DeclaredFields.Where(x => x.Name != "value__").Select(x => x.DefaultValue).ToArray() : throw new InvalidOperationException("Type is not an enumeration");

        // Initialize from specified type index in metadata

        // Top-level types
        public TypeInfo(int typeIndex, Assembly owner) : base(owner) {
            var pkg = Assembly.Model.Package;

            Definition = pkg.TypeDefinitions[typeIndex];
            Index = typeIndex;
            Namespace = pkg.Strings[Definition.namespaceIndex];
            Name = pkg.Strings[Definition.nameIndex];

            // Derived type?
            if (Definition.parentIndex >= 0)
                baseTypeUsage = Definition.parentIndex;

            // Nested type?
            if (Definition.declaringTypeIndex >= 0) {
                declaringTypeDefinitionIndex = (int) pkg.TypeUsages[Definition.declaringTypeIndex].datapoint;
                MemberType |= MemberTypes.NestedType;
            }

            // Generic type definition?
            if (Definition.genericContainerIndex >= 0) {
                IsGenericType = true;
                IsGenericParameter = false;
                IsGenericTypeDefinition = true; // All of our generic type parameters are unresolved
                ContainsGenericParameters = true;

                // Store the generic type parameters for later instantiation
                var container = pkg.GenericContainers[Definition.genericContainerIndex];

                GenericTypeParameters = pkg.GenericParameters.Skip((int) container.genericParameterStart).Take(container.type_argc).Select(p => new TypeInfo(this, p)).ToList();
            }

            // Add to global type definition list
            Assembly.Model.TypesByDefinitionIndex[Index] = this;
            Assembly.Model.TypesByFullName[FullName] = this;

            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_SERIALIZABLE) != 0)
                Attributes |= TypeAttributes.Serializable;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == Il2CppConstants.TYPE_ATTRIBUTE_PUBLIC)
                Attributes |= TypeAttributes.Public;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == Il2CppConstants.TYPE_ATTRIBUTE_NOT_PUBLIC)
                Attributes |= TypeAttributes.NotPublic;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == Il2CppConstants.TYPE_ATTRIBUTE_NESTED_PUBLIC)
                Attributes |= TypeAttributes.NestedPublic;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == Il2CppConstants.TYPE_ATTRIBUTE_NESTED_PRIVATE)
                Attributes |= TypeAttributes.NestedPrivate;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == Il2CppConstants.TYPE_ATTRIBUTE_NESTED_ASSEMBLY)
                Attributes |= TypeAttributes.NestedAssembly;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == Il2CppConstants.TYPE_ATTRIBUTE_NESTED_FAMILY)
                Attributes |= TypeAttributes.NestedFamily;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == Il2CppConstants.TYPE_ATTRIBUTE_NESTED_FAM_AND_ASSEM)
                Attributes |= TypeAttributes.NestedFamANDAssem;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == Il2CppConstants.TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM)
                Attributes |= TypeAttributes.NestedFamORAssem;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_ABSTRACT) != 0)
                Attributes |= TypeAttributes.Abstract;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_SEALED) != 0)
                Attributes |= TypeAttributes.Sealed;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_SPECIAL_NAME) != 0)
                Attributes |= TypeAttributes.SpecialName;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_IMPORT) != 0)
                Attributes |= TypeAttributes.Import;

            // TypeAttributes.Class == 0 so we only care about setting TypeAttributes.Interface (it's a non-interface class by default)
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_INTERFACE) != 0)
                Attributes |= TypeAttributes.Interface;

            // Enumerations - bit 1 of bitfield indicates this (also the baseTypeUsage will be System.Enum)
            if (((Definition.bitfield >> 1) & 1) == 1)
                enumUnderlyingTypeUsage = Definition.elementTypeIndex;

            // Pass-by-reference type
            // NOTE: This should actually always evaluate to false in the current implementation
            IsByRef = Index == Definition.byrefTypeIndex;

            // Add all implemented interfaces
            implementedInterfaceUsages = new int[Definition.interfaces_count];
            for (var i = 0; i < Definition.interfaces_count; i++)
                implementedInterfaceUsages[i] = pkg.InterfaceUsageIndices[Definition.interfacesStart + i];

            // Add all nested types
            declaredNestedTypes = new int[Definition.nested_type_count];
            for (var n = 0; n < Definition.nested_type_count; n++)
                declaredNestedTypes[n] = pkg.NestedTypeIndices[Definition.nestedTypesStart + n];

            // Add all fields
            for (var f = Definition.fieldStart; f < Definition.fieldStart + Definition.field_count; f++)
                DeclaredFields.Add(new FieldInfo(pkg, f, this));

            // Add all methods
            for (var m = Definition.methodStart; m < Definition.methodStart + Definition.method_count; m++) {
                var method = new MethodInfo(pkg, m, this);
                if (method.Name == ConstructorInfo.ConstructorName || method.Name == ConstructorInfo.TypeConstructorName)
                    DeclaredConstructors.Add(new ConstructorInfo(pkg, m, this));
                else
                    DeclaredMethods.Add(method);
            }

            // Add all properties
            for (var p = Definition.propertyStart; p < Definition.propertyStart + Definition.property_count; p++)
                DeclaredProperties.Add(new PropertyInfo(pkg, p, this));

            // There are rare cases when properties are only given as methods in the metadata
            // Find these and add them as properties


            // Add all events
            for (var e = Definition.eventStart; e < Definition.eventStart + Definition.event_count; e++)
                DeclaredEvents.Add(new EventInfo(pkg, e, this));
        }

        // Initialize type from binary usage
        // Much of the following is adapted from il2cpp::vm::Class::FromIl2CppType
        public TypeInfo(Il2CppModel model, Il2CppType pType, MemberTypes memberType) {
            var image = model.Package.BinaryImage;

            // Generic type unresolved and concrete instance types
            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST) {
                var generic = image.ReadMappedObject<Il2CppGenericClass>(pType.datapoint); // Il2CppGenericClass *
                var genericTypeDef = model.TypesByDefinitionIndex[generic.typeDefinitionIndex];

                Definition = model.Package.TypeDefinitions[generic.typeDefinitionIndex];
                Index = (int) generic.typeDefinitionIndex;

                Assembly = genericTypeDef.Assembly;
                Namespace = genericTypeDef.Namespace;
                Name = genericTypeDef.BaseName;
                Attributes |= TypeAttributes.Class;

                // Derived type?
                if (genericTypeDef.Definition.parentIndex >= 0)
                    baseTypeUsage = genericTypeDef.Definition.parentIndex;

                // Nested type?
                if (genericTypeDef.Definition.declaringTypeIndex >= 0) {
                    declaringTypeDefinitionIndex = (int)model.Package.TypeUsages[genericTypeDef.Definition.declaringTypeIndex].datapoint;
                    MemberType = memberType | MemberTypes.NestedType;
                }

                IsGenericType = true;
                IsGenericParameter = false;
                IsGenericTypeDefinition = false; // This is a use of a generic type definition
                ContainsGenericParameters = true;

                // Get the instantiation
                var genericInstance = image.ReadMappedObject<Il2CppGenericInst>(generic.context.class_inst);

                // Get list of pointers to type parameters (both unresolved and concrete)
                var genericTypeArguments = image.ReadMappedWordArray(genericInstance.type_argv, (int)genericInstance.type_argc);

                GenericTypeArguments = new List<TypeInfo>();

                foreach (var pArg in genericTypeArguments) {
                    var argType = model.GetTypeFromVirtualAddress((ulong) pArg);
                    // TODO: Detect whether unresolved or concrete (add concrete to GenericTypeArguments instead)
                    // TODO: GenericParameterPosition etc. in types we generate here
                    // TODO: Assembly etc.
                    GenericTypeArguments.Add(argType); // TODO: Fix MemberType here
                }
            }

            // TODO: Set DeclaringType for the two below

            // Array with known dimensions and bounds
            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_ARRAY) {
                var descriptor = image.ReadMappedObject<Il2CppArrayType>(pType.datapoint);
                ElementType = model.GetTypeFromVirtualAddress(descriptor.etype);

                Assembly = ElementType.Assembly;
                Definition = ElementType.Definition;
                Index = ElementType.Index;
                Namespace = ElementType.Namespace;
                Name = ElementType.Name;
                ContainsGenericParameters = ElementType.ContainsGenericParameters;

                IsArray = true;
                arrayRank = descriptor.rank;
            }

            // Dynamically allocated array or pointer type
            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY || pType.type == Il2CppTypeEnum.IL2CPP_TYPE_PTR) {
                ElementType = model.GetTypeFromVirtualAddress(pType.datapoint);

                Assembly = ElementType.Assembly;
                Definition = ElementType.Definition;
                Index = ElementType.Index;
                Namespace = ElementType.Namespace;
                Name = ElementType.Name;
                ContainsGenericParameters = ElementType.ContainsGenericParameters;

                IsPointer = (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_PTR);
                IsArray = !IsPointer;

                // Heap arrays always have one dimension
                arrayRank = 1;
            }

            // Generic type parameter
            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_VAR || pType.type == Il2CppTypeEnum.IL2CPP_TYPE_MVAR) {
                var paramType = model.Package.GenericParameters[pType.datapoint]; // genericParameterIndex
                var container = model.Package.GenericContainers[paramType.ownerIndex];

                var ownerType = model.TypesByDefinitionIndex[
                        container.is_method == 1
                        ? model.Package.Methods[container.ownerIndex].declaringType
                        : container.ownerIndex];

                Assembly = ownerType.Assembly;
                Namespace = "";
                Name = model.Package.Strings[paramType.nameIndex];
                Attributes |= TypeAttributes.Class;

                // Derived type?
                if (ownerType.Definition.parentIndex >= 0)
                    baseTypeUsage = ownerType.Definition.parentIndex;

                // Nested type always - sets DeclaringType used below
                declaringTypeDefinitionIndex = ownerType.Index;
                MemberType = memberType | MemberTypes.NestedType;

                // All generic method type parameters have a declared method
                if (container.is_method == 1)
                    DeclaringMethod = model.MethodsByDefinitionIndex[container.ownerIndex];

                IsGenericParameter = true;
                ContainsGenericParameters = true;
                IsGenericType = false;
                IsGenericTypeDefinition = false;
            }
        }

        // Initialize a type that is a generic parameter of a generic type
        // See: https://docs.microsoft.com/en-us/dotnet/api/system.type.isgenerictype?view=netframework-4.8
        public TypeInfo(TypeInfo declaringType, Il2CppGenericParameter param) : base(declaringType) {
            // Same visibility attributes as declaring type
            Attributes = declaringType.Attributes;

            // Same namespace as declaring type
            Namespace = declaringType.Namespace;

            // Special constraints
            GenericParameterAttributes = (GenericParameterAttributes) param.flags;

            // Type constraints
            genericConstraintIndex = param.constraintsStart;
            genericConstraintCount = param.constraintsCount;

            // Base type of object (set by default)
            // TODO: BaseType should be set to base type constraint
            // TODO: ImplementedInterfaces should be set to interface types constraints

            // Name of parameter
            Name = Assembly.Model.Package.Strings[param.nameIndex];

            // Position
            GenericParameterPosition = param.num;

            IsGenericParameter = true;
            IsGenericType = false;
            IsGenericTypeDefinition = false;
            ContainsGenericParameters = true;
        }

        // Initialize a type that is a generic parameter of a generic method
        public TypeInfo(MethodBase declaringMethod, Il2CppGenericParameter param) : this(declaringMethod.DeclaringType, param)
            => DeclaringMethod = declaringMethod;

        // Initialize a type that is a reference to the specified type
        private TypeInfo(TypeInfo underlyingType) {
            ElementType = underlyingType;
            IsByRef = true;

            // No base type or declaring type for reference types
            Assembly = ElementType.Assembly;
            Definition = ElementType.Definition;
            Index = ElementType.Index;
            Namespace = ElementType.Namespace;
            Name = ElementType.Name;

            Attributes = ElementType.Attributes;
        }

        public TypeInfo MakeByRefType() => new TypeInfo(this);

        // Get all the other types directly referenced by this type (single level depth; no recursion)
        public List<TypeInfo> GetAllTypeReferences() {
            var refs = new HashSet<TypeInfo>();

            // Fixed attributes
            if (IsImport)
                refs.Add(Assembly.Model.TypesByFullName["System.Runtime.InteropServices.ComVisibleAttribute"]);
            if (IsSerializable)
                refs.Add(Assembly.Model.TypesByFullName["System.SerializableAttribute"]);

            // Constructor, event, field, method, nested type, property attributes
            var attrs = DeclaredMembers.SelectMany(m => m.CustomAttributes);
            refs.UnionWith(attrs.Select(a => a.AttributeType));

            // Events
            refs.UnionWith(DeclaredEvents.Select(e => e.EventHandlerType));

            // Fields
            refs.UnionWith(DeclaredFields.Select(f => f.FieldType));

            // Properties (return type of getters or argument type of setters)
            refs.UnionWith(DeclaredProperties.Select(p => p.PropertyType));

            // Nested types
            refs.UnionWith(DeclaredNestedTypes);
            refs.UnionWith(DeclaredNestedTypes.SelectMany(n => n.GetAllTypeReferences()));

            // Constructors
            refs.UnionWith(DeclaredConstructors.SelectMany(m => m.DeclaredParameters).Select(p => p.ParameterType));

            // Methods (includes event add/remove/raise, property get/set methods and extension methods)
            refs.UnionWith(DeclaredMethods.Select(m => m.ReturnParameter.ParameterType));
            refs.UnionWith(DeclaredMethods.SelectMany(m => m.DeclaredParameters).Select(p => p.ParameterType));

            // Method generic type parameters and constraints
            refs.UnionWith(DeclaredMethods.SelectMany(m => m.GenericTypeParameters ?? new List<TypeInfo>()));
            refs.UnionWith(DeclaredMethods.SelectMany(m => m.GenericTypeParameters ?? new List<TypeInfo>())
                .SelectMany(p => p.GetGenericParameterConstraints()));

            // Type declaration attributes
            refs.UnionWith(CustomAttributes.Select(a => a.AttributeType));

            // Parent type
            if (BaseType != null)
                refs.Add(BaseType);

            // Declaring type
            if (DeclaringType != null)
                refs.Add(DeclaringType);

            // Element type
            if (HasElementType)
                refs.Add(ElementType);

            // Enum type
            if (IsEnum)
                refs.Add(GetEnumUnderlyingType());

            // Generic type parameters and constraints
            if (GenericTypeParameters != null)
                refs.UnionWith(GenericTypeParameters);
            if (GenericTypeArguments != null)
                refs.UnionWith(GenericTypeArguments);
            refs.UnionWith(GetGenericParameterConstraints());

            // Generic type constraints of type parameters in generic type definition
            if (GenericTypeParameters != null)
                refs.UnionWith(GenericTypeParameters.SelectMany(p => p.GetGenericParameterConstraints()));

            // Implemented interfaces
            refs.UnionWith(ImplementedInterfaces);

            // Repeatedly replace arrays, pointers and references with their element types
            while (refs.Any(r => r.HasElementType))
                refs = refs.Select(r => r.HasElementType ? r.ElementType : r).ToHashSet();

            // Type arguments in generic types that may have been a field, method parameter etc.
            IEnumerable<TypeInfo> genericArguments = refs.ToList();
            do {
                genericArguments = genericArguments.Where(r => r.GenericTypeArguments != null).SelectMany(r => r.GenericTypeArguments);
                refs.UnionWith(genericArguments);
            } while (genericArguments.Any());

            // Remove anonymous types
            refs.RemoveWhere(r => string.IsNullOrEmpty(r.FullName));

            IEnumerable<TypeInfo> refList = refs;

            // Eliminated named duplicates (the HashSet removes instance duplicates)
            refList = refList.GroupBy(r => r.FullName).Select(p => p.First());

            // Remove System.Object
            refList = refList.Where(r => r.FullName != "System.Object");

            return refList.ToList();
        }

        // Display name of object
        public override string Name => IsGenericParameter ? base.Name :
            (HasElementType? ElementType.Name :
                (DeclaringType != null ? DeclaringType.Name + "+" : "")
                + base.Name
                + (GenericTypeParameters != null ? "[" + string.Join(",", GenericTypeParameters.Select(x => x.Namespace != Namespace? x.FullName ?? x.Name : x.Name)) + "]" : "")
                + (GenericTypeArguments != null ? "[" + string.Join(",", GenericTypeArguments.Select(x => x.Namespace != Namespace? x.FullName ?? x.Name : x.Name)) + "]" : ""))
            + (IsArray ? "[" + new string(',', GetArrayRank() - 1) + "]" : "")
            + (IsByRef ? "&" : "")
            + (IsPointer ? "*" : "");

        public string GetAccessModifierString() => this switch {
            { IsPublic: true } => "public ",
            { IsNotPublic: true } => "internal ",

            { IsNestedPublic: true } => "public ",
            { IsNestedPrivate: true } => "private ",
            { IsNestedFamily: true } => "protected ",
            { IsNestedAssembly: true } => "internal ",
            { IsNestedFamORAssem: true } => "protected internal ",
            { IsNestedFamANDAssem: true } => "private protected ",
            _ => throw new InvalidOperationException("Unknown type access modifier")
        };

        public string GetModifierString() {
            var modifiers = new StringBuilder(GetAccessModifierString());

            // An abstract sealed class is a static class
            if (IsAbstract && IsSealed)
                modifiers.Append("static ");
            else {
                if (IsAbstract && !IsInterface)
                    modifiers.Append("abstract ");
                if (IsSealed && !IsValueType && !IsEnum)
                    modifiers.Append("sealed ");
            }
            if (IsInterface)
                modifiers.Append("interface ");
            else if (IsValueType)
                modifiers.Append("struct ");
            else if (IsEnum)
                modifiers.Append("enum ");
            else
                modifiers.Append("class ");

            return modifiers.ToString();
        }

        public string GetTypeConstraintsString(Scope scope) {
            if (!IsGenericParameter)
                return string.Empty;

            var typeConstraints = GetGenericParameterConstraints();
            if (GenericParameterAttributes == GenericParameterAttributes.None && typeConstraints.Length == 0)
                return string.Empty;

            // Check if we are in a nested type, and if so, exclude ourselves if we are a generic type parameter from the outer type
            // All constraints are inherited automatically by all nested types so we only have to look at the immediate outer type
            if (DeclaringMethod == null && DeclaringType.IsNested && (DeclaringType.DeclaringType.GenericTypeParameters?.Any(p => p.Name == Name) ?? false))
                return string.Empty;

            // Check if we are in an overriding method, and if so, exclude ourselves if we are a generic type parameter from the base method
            // All constraints are inherited automatically by all overriding methods so we only have to look at the immediate base method
            if (DeclaringMethod != null && DeclaringMethod.IsVirtual && !DeclaringMethod.IsAbstract && !DeclaringMethod.IsFinal
                && (DeclaringMethod.Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.ReuseSlot) {
                // Find nearest ancestor base method which has us as a generic type parameter
                var sig = DeclaringMethod.GetSignatureString();
                var method = DeclaringMethod.DeclaringType.BaseType.GetAllMethods()
                    .FirstOrDefault(m => m.IsHideBySig && m.IsVirtual && m.GetSignatureString() == sig && (m.GenericTypeParameters?.Any(p => p.Name == Name) ?? false));

                // Stop if we are inherited from a base method
                if (method != null)
                    return string.Empty;
            }

            var constraintList = typeConstraints.Where(c => c.FullName != "System.ValueType").Select(c => c.GetScopedCSharpName(scope)).ToList();

            // struct or class must be the first constraint specified
            if ((GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) == GenericParameterAttributes.NotNullableValueTypeConstraint)
                constraintList.Insert(0, "struct");
            if ((GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) == GenericParameterAttributes.ReferenceTypeConstraint)
                constraintList.Insert(0, "class");

            if ((GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) == GenericParameterAttributes.DefaultConstructorConstraint
                && !constraintList.Contains("struct"))
                // new() must be the last constraint specified
                constraintList.Add("new()");

            // Covariance/contravariance constraints can lead to an empty constraint list
            if (!constraintList.Any())
                return string.Empty;

            return "where " + Name + " : " + string.Join(", ", constraintList);
        }

        public override string ToString() => Name;
    }
}