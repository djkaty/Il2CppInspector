/*
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Il2CppInspector.Reflection
{
    public class TypeInfo : MemberInfo
    {
        // IL2CPP-specific data
        public Il2CppTypeDefinition Definition { get; }
        public int Index { get; } = -1;

        // This dictionary will cache all instantiated generic types out of this definition.
        // Only valid for GenericTypeDefinition - not valid on instantiated types!
        private Dictionary<TypeInfo[], TypeInfo> genericTypeInstances;
        public class TypeArgumentsComparer : EqualityComparer<TypeInfo[]>
        {
            public override bool Equals(TypeInfo[] x, TypeInfo[] y) {
                return ((IStructuralEquatable)x).Equals(y, StructuralComparisons.StructuralEqualityComparer);
            }

            public override int GetHashCode(TypeInfo[] obj) {
                return ((IStructuralEquatable)obj).GetHashCode(StructuralComparisons.StructuralEqualityComparer);
            }
        }


        // Cached derived types
        private Dictionary<int, TypeInfo> generatedArrayTypes = new Dictionary<int, TypeInfo>();
        private TypeInfo generatedByRefType;
        private TypeInfo generatedPointerType;

        // Information/flags about the type
        // Undefined if the Type represents a generic type parameter
        public TypeAttributes Attributes { get; }

        public TypeInfo BaseType {
            get {
                if (IsPointer || IsByRef)
                    return null;
                if (IsArray)
                    return Assembly.Model.TypesByFullName["System.Array"];
                if (Definition != null) {
                    if (Definition.parentIndex >= 0)
                        return Assembly.Model.TypesByReferenceIndex[Definition.parentIndex];
                }
                if (genericTypeDefinition != null) {
                    return genericTypeDefinition.BaseType.SubstituteGenericArguments(genericArguments);
                }
                if (IsGenericParameter) {
                    var res = GetGenericParameterConstraints().Where(t => !t.IsInterface).FirstOrDefault();
                    if (res != null)
                        return res;
                }
                if (Namespace != "System" || BaseName != "Object")
                    return Assembly.Model.TypesByFullName["System.Object"];
                return null;
            }
        }

        public override TypeInfo DeclaringType {
            get {
                if (Definition != null) {
                    /* Type definition */
                    if (Definition.declaringTypeIndex == -1)
                        return null;
                    var type = Assembly.Model.TypesByReferenceIndex[Definition.declaringTypeIndex];
                    if (type == null) {
                        /* This might happen while initially setting up the types */
                        var typeRef = Assembly.Model.Package.TypeReferences[Definition.declaringTypeIndex];
                        type = Assembly.Model.TypesByDefinitionIndex[(int)typeRef.datapoint];
                    }
                    return type;
                }
                if (genericTypeDefinition != null) {
                    // Generic parameters are *not* substituted in the DeclaringType
                    return genericTypeDefinition.DeclaringType;
                }
                return base.DeclaringType;
            }
        }

        // True if the type contains unresolved generic type parameters
        public bool ContainsGenericParameters => (HasElementType && ElementType.ContainsGenericParameters) || IsGenericParameter || genericArguments.Any(ga => ga.ContainsGenericParameters);

        // Custom attributes for this member
        public override IEnumerable<CustomAttributeData> CustomAttributes => CustomAttributeData.GetCustomAttributes(genericTypeDefinition ?? this);

        private List<ConstructorInfo> declaredConstructors;
        public ReadOnlyCollection<ConstructorInfo> DeclaredConstructors {
            get {
                if (declaredConstructors != null)
                    return declaredConstructors.AsReadOnly();
                if (genericTypeDefinition != null) {
                    var result = genericTypeDefinition.DeclaredConstructors.Select(c => new ConstructorInfo(c, this)).ToList();
                    declaredConstructors = result;
                    return result.AsReadOnly();
                }
                return new List<ConstructorInfo>().AsReadOnly();
            }
        }

        private List<EventInfo> declaredEvents;
        public ReadOnlyCollection<EventInfo> DeclaredEvents {
            get {
                if (declaredEvents != null)
                    return declaredEvents.AsReadOnly();
                if (genericTypeDefinition != null) {
                    var result = genericTypeDefinition.DeclaredEvents.Select(c => new EventInfo(c, this)).ToList();
                    declaredEvents = result;
                    return result.AsReadOnly();
                }
                return new List<EventInfo>().AsReadOnly();
            }
        }

        private List<FieldInfo> declaredFields;
        public ReadOnlyCollection<FieldInfo> DeclaredFields {
            get {
                if (declaredFields != null)
                    return declaredFields.AsReadOnly();
                if (genericTypeDefinition != null) {
                    var result = genericTypeDefinition.DeclaredFields.Select(c => new FieldInfo(c, this)).ToList();
                    declaredFields = result;
                    return result.AsReadOnly();
                }
                return new List<FieldInfo>().AsReadOnly();
            }
        }

        public List<MemberInfo> DeclaredMembers => new IEnumerable<MemberInfo>[] {
            DeclaredConstructors, DeclaredEvents, DeclaredFields, DeclaredMethods,
            DeclaredNestedTypes, DeclaredProperties
        }.SelectMany(m => m).ToList();

        private List<MethodInfo> declaredMethods;
        public ReadOnlyCollection<MethodInfo> DeclaredMethods {
            get {
                if (declaredMethods != null)
                    return declaredMethods.AsReadOnly();
                if (genericTypeDefinition != null) {
                    var result = genericTypeDefinition.DeclaredMethods.Select(c => new MethodInfo(c, this)).ToList();
                    declaredMethods = result;
                    return result.AsReadOnly();
                }
                return new List<MethodInfo>().AsReadOnly();
            }
        }

        private readonly TypeRef[] declaredNestedTypes;
        public IEnumerable<TypeInfo> DeclaredNestedTypes {
            get {
                if (declaredNestedTypes != null)
                    return declaredNestedTypes.Select(x => x.Value);
                /* Type parameters are not substituted into nested classes,
                 * as nested classes aren't required to use the parameters
                 * from the containing class.
                 * This also matches the behaviour of the C# reflection API.
                 */
                if (genericTypeDefinition != null)
                    return genericTypeDefinition.DeclaredNestedTypes;
                return Enumerable.Empty<TypeInfo>();
            }
        }

        private List<PropertyInfo> declaredProperties;
        public ReadOnlyCollection<PropertyInfo> DeclaredProperties {
            get {
                if (declaredProperties != null)
                    return declaredProperties.AsReadOnly();
                if (genericTypeDefinition != null) {
                    var result = genericTypeDefinition.DeclaredProperties.Select(c => new PropertyInfo(c, this)).ToList();
                    declaredProperties = result;
                    return result.AsReadOnly();
                }
                return new List<PropertyInfo>().AsReadOnly();
            }
        }

        // Get a field by its name
        public FieldInfo GetField(string name) => DeclaredFields.FirstOrDefault(f => f.Name == name);

        private TypeRef[] genericParameterConstraints;

        // Get type constraints on a generic parameter
        public TypeInfo[] GetGenericParameterConstraints() => genericParameterConstraints?.Select(t => t.Value)?.ToArray() ?? Array.Empty<TypeInfo>();

        private readonly TypeInfo genericTypeDefinition;

        /* https://docs.microsoft.com/en-us/dotnet/api/system.type.getgenerictypedefinition?view=netframework-4.8 */
        public TypeInfo GetGenericTypeDefinition() {
            if (genericTypeDefinition != null)
                return genericTypeDefinition;
            if (IsGenericTypeDefinition)
                return this;
            throw new InvalidOperationException("This method can only be called on generic types");
        }

        public ConstructorInfo GetConstructorByDefinition(ConstructorInfo definition) {
            if (genericTypeDefinition != null) {
                var collection = genericTypeDefinition.DeclaredConstructors;
                for (int i = 0; i < collection.Count; i++) {
                    if (collection[i].RootDefinition == definition.RootDefinition)
                        return DeclaredConstructors[i];
                }
            }
            return definition;
        }

        // Get a method or constructor by the base type definition of that method
        public MethodInfo GetMethodByDefinition(MethodInfo definition) {
            if (genericTypeDefinition != null) {
                var collection = genericTypeDefinition.DeclaredMethods;
                for (int i = 0; i < collection.Count; i++) {
                    if (collection[i].RootDefinition == definition.RootDefinition)
                        return DeclaredMethods[i];
                }
            }
            return definition;
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

        public MethodBase[] GetVTable() {
            if (Definition != null) {
                MetadataUsage[] vt = Assembly.Model.Package.GetVTable(Definition);
                MethodBase[] res = new MethodBase[vt.Length];
                for (int i = 0; i < vt.Length; i++) {
                    if (vt[i] != null)
                        res[i] = Assembly.Model.GetMetadataUsageMethod(vt[i]);
                }
                return res;
            } else if (genericTypeDefinition != null) {
                MethodBase[] baseVt = genericTypeDefinition.GetVTable();
                MethodBase[] res = new MethodBase[baseVt.Length];
                for (int i = 0; i < baseVt.Length; i++) {
                    if (baseVt[i] == null)
                        continue;
                    var declaringType = baseVt[i].DeclaringType.SubstituteGenericArguments(genericArguments);
                    if (baseVt[i] is ConstructorInfo ci)
                        res[i] = declaringType.GetConstructorByDefinition(ci);
                    else
                        res[i] = declaringType.GetMethodByDefinition((MethodInfo)baseVt[i]);
                }
                return res;
            }
            return Array.Empty<MethodBase>();
        }

        // Method that the type is declared in if this is a type parameter of a generic method
        // TODO: Make a unit test from this: https://docs.microsoft.com/en-us/dotnet/api/system.type.declaringmethod?view=netframework-4.8
        public MethodBase DeclaringMethod { get; }

        // IsGenericTypeParameter and IsGenericMethodParameter from https://github.com/dotnet/corefx/issues/23883
        public bool IsGenericTypeParameter => IsGenericParameter && DeclaringMethod == null;
        public bool IsGenericMethodParameter => IsGenericParameter && DeclaringMethod != null;

        // Gets the type of the object encompassed or referred to by the current array, pointer or reference type
        public TypeInfo ElementType { get; }

        #region Names
        public string BaseName => base.Name;

        private static string unmangleName(string name) {
            var index = name.IndexOf("`", StringComparison.Ordinal);
            if (index != -1)
                name = name.Remove(index);
            return name;
        }

        // Get rid of generic backticks
        public string UnmangledBaseName => unmangleName(base.Name);

        // C# colloquial name of the type (if available)
        public string CSharpName {
            get {
                if (HasElementType) {
                    var n = ElementType.CSharpName;
                    if (IsByRef)
                        n = "ref " + n;
                    if (IsArray)
                        n += "[" + new string(',', GetArrayRank() - 1) + "]";
                    if (IsPointer)
                        n += "*";
                    return n;
                } else {
                    var s = Namespace + "." + base.Name;
                    var i = Il2CppConstants.FullNameTypeString.IndexOf(s);
                    var n = (i != -1 ? Il2CppConstants.CSharpTypeString[i] : base.Name);
                    n = unmangleName(n);
                    var ga = GetGenericArguments();
                    if (ga.Any())
                        n += "<" + string.Join(", ", ga.Select(x => x.CSharpName)) + ">";
                    if (s == "System.Nullable`1" && ga.Any())
                        n = ga[0].CSharpName + "?";
                    return n;
                }
            }
        }

        // C# name as it would be written in a type declaration
        public string GetCSharpTypeDeclarationName(bool includeVariance = false) {
            if (HasElementType) {
                var n = ElementType.GetCSharpTypeDeclarationName();
                if (IsByRef)
                    n = "ref " + n;
                if (IsArray)
                    n += "[" + new string(',', GetArrayRank() - 1) + "]";
                if (IsPointer)
                    n += "*";
                return n;
            } else {
                var n = unmangleName(base.Name);
                var ga = IsNested ? GetGenericArguments().Where(p => DeclaringType.GetGenericArguments().All(dp => dp.Name != p.Name)) : GetGenericArguments();
                if (ga.Any())
                    n += "<" + string.Join(", ", ga.Select(x => (!x.IsGenericTypeParameter ? x.Namespace + "." : "") + x.GetCSharpTypeDeclarationName(includeVariance: true))) + ">";
                if (includeVariance) {
                    if ((GenericParameterAttributes & GenericParameterAttributes.Covariant) == GenericParameterAttributes.Covariant)
                        n = "out " + n;
                    if ((GenericParameterAttributes & GenericParameterAttributes.Contravariant) == GenericParameterAttributes.Contravariant)
                        n = "in " + n;
                }
                return n;
            }
        }

        // Display name of object
        public override string Name {
            get {
                if (IsGenericParameter)
                    return base.Name;
                if (HasElementType) {
                    var n = ElementType.Name;
                    if (IsArray)
                        n += "[" + new string(',', GetArrayRank() - 1) + "]";
                    if (IsByRef)
                        n += "&";
                    if (IsPointer)
                        n += "*";
                    return n;
                } else {
                    /* XXX This is not exactly accurate to C# Type.Name:
                     * Type.Name should be the bare name (with & * [] suffixes)
                     * but without nested types or generic arguments */
                    var n = base.Name;
                    if (DeclaringType != null)
                        n = DeclaringType.Name + "+" + n;
                    var ga = GetGenericArguments();
                    if (ga.Any())
                        n += "[" + string.Join(",", ga.Select(x => x.Namespace != Namespace ? x.FullName ?? x.Name : x.Name)) + "]";
                    return n;
                }
            }
        }

        // Type name including namespace
        // Fully qualified generic type names from the C# compiler use backtick and arity rather than a list of generic arguments
        public string FullName {
            get {
                if (IsGenericParameter)
                    return null;
                if (HasElementType) {
                    var n = ElementType.FullName;
                    if (n == null)
                        return null;
                    if (IsArray)
                        n += "[" + new string(',', GetArrayRank() - 1) + "]";
                    if (IsByRef)
                        n += "&";
                    if (IsPointer)
                        n += "*";
                    return n;
                } else {
                    var n = base.Name;
                    if (DeclaringType != null)
                        n = DeclaringType.FullName + "+" + n;
                    else if (Namespace.Length > 0)
                        n = Namespace + "." + n;
                    return n;
                }
            }
        }

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
                usingScopeIsChildOfDeclaringScope ? declaringScope

                // Check to see if there is a namespace in our using directives which brings this type into scope
                // Sort by descending order of length to search the deepest namespaces first
                : scope.Namespaces?.OrderByDescending(n => n.Length).FirstOrDefault(n => declaringScope == n || declaringScope.StartsWith(n + "."));

            // minimallyScopedName will eventually contain the least qualified name needed to access the type
            // Initially we set it as follows:
            // - The non-mutual part of the declaring scope if there is a mutual root scope
            // - The fully-qualified type name if there is no mutual root scope
            // - The leaf name if the declaring scope and mutual root scope are the same
            // The first two must be checked in this order to avoid a . at the start
            // when the mutual root scope and declaring scope are both empty
            var minimallyScopedName =
                    declaringScope == mutualRootScope ? base.Name :
                    string.IsNullOrEmpty(mutualRootScope) ? declaringScope + '.' + base.Name :
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
            var nsAndTypeHierarchy = usingScopeIsChildOfDeclaringScope ?
                usingDirective.Split('.').Append(minimallyScopedName).ToArray()
                : new[] { outerTypeName };

            var hidden = true;
            var foundTypeInAncestorScope = false;
            string testTypeName = "";

            for (var depth = nsAndTypeHierarchy.Length - 1; depth >= 0 && hidden; depth--) {
                testTypeName = nsAndTypeHierarchy[depth] + (testTypeName.Length > 0 ? "." : "") + testTypeName;

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
                    minimallyScopedName = usingScopeIsChildOfDeclaringScope ? testTypeName : Name.Replace('+', '.');

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
                var matchingNamespaces = scope.Namespaces?
                    .Where(n => Assembly.Model.TypesByFullName.ContainsKey(n + "." + outerTypeName)).ToList() ?? new List<string>();

                // The global namespace is in scope so take every matching type from that too
                if (Assembly.Model.TypesByFullName.ContainsKey(outerTypeName))
                    matchingNamespaces.Add("");

                // More than one possible matching namespace? If so, the type reference is ambiguous
                if (matchingNamespaces.Count > 1) {
                    // TODO: This can be improved to cut off a new mutual root that doesn't cause ambiguity
                    minimallyScopedName = usedType;
                }

                // No matching namespaces, not hidden, no mutual root scope in the file and no using directive?
                // If so, the type's namespace is completely out of scope so use the fully-qualified type name
                if (matchingNamespaces.Count == 0 && !hidden && string.IsNullOrEmpty(mutualRootScope) && usingDirective == null)
                    minimallyScopedName = usedType;
            }

            // Finally, check if the selected name has ambiguity with any available namespaces in the current scope
            // If so, use the full name with the mutual root scope cut off from the start
            var checkNamespaces = scope.Namespaces?
                .Select(ns => (!string.IsNullOrEmpty(ns)? ns + "." : "") + minimallyScopedName).ToList() ?? new List<string>();

            if (Assembly.Model.Namespaces.Intersect(checkNamespaces).Any())
                minimallyScopedName = mutualRootScope.Length > 0 ? usedType.Substring(mutualRootScope.Length + 1) : usedType;

            // Check current namespace and all ancestors too
            else {
                checkNamespaces.Clear();
                var ancestorUsingScope = "." + usingScope;
                while (ancestorUsingScope.IndexOf(".", StringComparison.Ordinal) != -1) {
                    ancestorUsingScope = ancestorUsingScope.Substring(0, ancestorUsingScope.LastIndexOf(".", StringComparison.Ordinal));
                    checkNamespaces.Add((ancestorUsingScope.Length > 0 ? ancestorUsingScope.Substring(1) + "." : "") + minimallyScopedName);
                }

                if (Assembly.Model.Namespaces.Intersect(checkNamespaces).Any())
                    minimallyScopedName = mutualRootScope.Length > 0 ? usedType.Substring(mutualRootScope.Length + 1) : "global::" + usedType;
            }

            return minimallyScopedName;
        }

        // C#-friendly type name as it should be used in the scope of a given type
        public string GetScopedCSharpName(Scope usingScope = null, bool omitRef = false, bool isPartOfTypeDeclaration = false) {
            // Unscoped name if no using scope specified
            if (usingScope == null)
                return CSharpName;

            // Generic parameters don't have a scope
            if (IsGenericParameter)
                return CSharpName;

            var s = Namespace + "." + base.Name;

            // Built-in keyword type names do not require a scope
            var i = Il2CppConstants.FullNameTypeString.IndexOf(s);
            var n = i != -1 ? Il2CppConstants.CSharpTypeString[i] : getScopedFullName(usingScope);
            n = unmangleName(n);

            // Generic type parameters and type arguments
            // Inheriting from a base class or implementing an interface use the type's declaring scope, not the type's scope itself
            // for generic type parameters
            var outerScope = usingScope;
            if (isPartOfTypeDeclaration)
                outerScope = new Scope { Current = usingScope.Current?.DeclaringType, Namespaces = usingScope.Namespaces };

            var g = string.Join(", ", getGenericTypeParameters(usingScope).Select(x => x.GetScopedCSharpName(outerScope)));
            if (!string.IsNullOrEmpty(g))
                n += "<" + g + ">";

            // Nullable types
            if (s == "System.Nullable`1" && GetGenericArguments().Any())
                n = GetGenericArguments()[0].GetScopedCSharpName(usingScope) + "?";

            // Arrays, pointers, references
            if (HasElementType)
                n = ElementType.GetScopedCSharpName(usingScope);

            return (IsByRef && !omitRef ? "ref " : "") + n + (IsArray ? "[" + new string(',', GetArrayRank() - 1) + "]" : "") + (IsPointer ? "*" : "");
        }
        #endregion

        // Get the generic type parameters for a specific usage of this type based on its scope,
        // or all generic type parameters if no scope specified
        private IEnumerable<TypeInfo> getGenericTypeParameters(Scope scope = null) {
            var ga = GetGenericArguments();

            // If no scope or empty scope specified, or no type parameters, stop here
            if (scope?.Current == null || !ga.Any())
                return ga;

            // In order to elide generic type parameters, the using scope must be a parent of the declaring scope
            // Determine if the using scope is a parent of the declaring scope (always a child if using scope is empty)
            var usingScopeIsParent = false;
            for (var s = DeclaringType; s != null && !usingScopeIsParent; s = s.DeclaringType)
                if (s == scope.Current)
                    usingScopeIsParent = true;

            if (!usingScopeIsParent)
                return ga;

            // Get the generic type parameters available in the using scope
            // (no need to recurse because every nested type inherits all of the generic type parameters of all of its ancestors)
            var gasInScope = scope.Current.GetGenericArguments();

            // Return all of the generic type parameters this type uses minus those already in scope
            return ga.Where(p => gasInScope.All(pp => pp.Name != p.Name));
        }

        public GenericParameterAttributes GenericParameterAttributes { get; }

        // Generic parameter position in list of non-concrete type parameters
        // See: https://docs.microsoft.com/en-us/dotnet/api/system.type.genericparameterposition?view=netframework-4.8
        private int genericParameterPosition;
        public int GenericParameterPosition {
            get => IsGenericParameter ? genericParameterPosition : throw new InvalidOperationException("The current type does not represent a type parameter");
            private set => genericParameterPosition = value;
        }

        // For a generic type definition: the list of generic type parameters
        // For an open generic type: a mix of generic type parameters and generic type arguments
        // For a closed generic type: the list of generic type arguments
        private readonly TypeInfo[] genericArguments = Array.Empty<TypeInfo>();

        public TypeInfo[] GenericTypeParameters => IsGenericTypeDefinition ? genericArguments : Array.Empty<TypeInfo>();

        public TypeInfo[] GenericTypeArguments => !IsGenericTypeDefinition ? genericArguments : Array.Empty<TypeInfo>();

        // See: https://docs.microsoft.com/en-us/dotnet/api/system.type.getgenericarguments?view=netframework-4.8
        public TypeInfo[] GetGenericArguments() => genericArguments;

        // True if an array, pointer or reference, otherwise false
        // See: https://docs.microsoft.com/en-us/dotnet/api/system.type.haselementtype?view=netframework-4.8
        public bool HasElementType => ElementType != null;

        private readonly TypeRef[] implementedInterfaceReferences;
        public IEnumerable<TypeInfo> ImplementedInterfaces {
            get {
                if (Definition != null)
                    return implementedInterfaceReferences.Select(x => x.Value);
                if (genericTypeDefinition != null)
                    return genericTypeDefinition.ImplementedInterfaces.Select(t => t.SubstituteGenericArguments(genericArguments));
                if (IsGenericParameter)
                    return GetGenericParameterConstraints().Where(t => t.IsInterface);
                return Enumerable.Empty<TypeInfo>();
            }
        }

        // Get only interfaces not inherited from base interfaces
        public IEnumerable<TypeInfo> NonInheritedInterfaces => ImplementedInterfaces.Except(ImplementedInterfaces.SelectMany(t => t.ImplementedInterfaces));

        public bool IsAbstract => (Attributes & TypeAttributes.Abstract) == TypeAttributes.Abstract;
        public bool IsArray { get; }
        public bool IsByRef { get; }
        public bool IsClass => (Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class;
        public bool IsEnum { get; }
        public bool IsGenericParameter { get; }
        public bool IsGenericType { get; }
        public bool IsGenericTypeDefinition => (Definition != null) && genericArguments.Any();
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

        public string[] GetEnumNames() => IsEnum ? DeclaredFields.Where(x => x.Name != "value__").Select(x => x.Name).ToArray() : throw new InvalidOperationException("Type is not an enumeration");

        // The underlying type of an enumeration (int by default)
        private readonly TypeRef enumUnderlyingTypeReference = null;

        public TypeInfo GetEnumUnderlyingType() {
            if (!IsEnum)
                return null;
            return enumUnderlyingTypeReference.Value;
        }

        public Array GetEnumValues() => IsEnum ? DeclaredFields.Where(x => x.Name != "value__").Select(x => x.DefaultValue).ToArray() : throw new InvalidOperationException("Type is not an enumeration");

        // Initialize type from TypeDef using specified index in metadata
        public TypeInfo(int typeIndex, Assembly owner) : base(owner) {
            var pkg = Assembly.Model.Package;

            Definition = pkg.TypeDefinitions[typeIndex];
            Index = typeIndex;
            Namespace = pkg.Strings[Definition.namespaceIndex];
            Name = pkg.Strings[Definition.nameIndex];

            // Nested type?
            if (Definition.declaringTypeIndex >= 0) {
                MemberType |= MemberTypes.NestedType;
            }

            // Add to global type definition list
            Assembly.Model.TypesByDefinitionIndex[Index] = this;

            // Generic type definition?
            if (Definition.genericContainerIndex >= 0) {
                IsGenericType = true;
                IsGenericParameter = false;

                // Store the generic type parameters for later instantiation
                var container = pkg.GenericContainers[Definition.genericContainerIndex];

                genericArguments = Enumerable.Range((int)container.genericParameterStart, container.type_argc)
                    .Select(index => Assembly.Model.GetGenericParameterType(index)).ToArray();
                genericTypeInstances = new Dictionary<TypeInfo[], TypeInfo>(new TypeArgumentsComparer());
                genericTypeInstances[genericArguments] = this;
            }

            // TODO: Move this to after we've populated TypesByReferenceIndex, since FullName might touch that
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

            // Enumerations - bit 1 of bitfield indicates this (also the baseTypeReference will be System.Enum)
            if (((Definition.bitfield >> 1) & 1) == 1) {
                IsEnum = true;
                enumUnderlyingTypeReference = TypeRef.FromReferenceIndex(Assembly.Model, Definition.elementTypeIndex);
            }

            // Pass-by-reference type
            // NOTE: This should actually always evaluate to false in the current implementation
            // This field is no longer present in metadata v27
            // IsByRef = Index == Definition.byrefTypeIndex;
            IsByRef = false;

            // Add all implemented interfaces
            implementedInterfaceReferences = new TypeRef[Definition.interfaces_count];
            for (var i = 0; i < Definition.interfaces_count; i++)
                implementedInterfaceReferences[i] = TypeRef.FromReferenceIndex(Assembly.Model, pkg.InterfaceUsageIndices[Definition.interfacesStart + i]);

            // Add all nested types
            declaredNestedTypes = new TypeRef[Definition.nested_type_count];
            for (var n = 0; n < Definition.nested_type_count; n++)
                declaredNestedTypes[n] = TypeRef.FromDefinitionIndex(Assembly.Model, pkg.NestedTypeIndices[Definition.nestedTypesStart + n]);

            // Add all fields
            declaredFields = new List<FieldInfo>();
            for (var f = Definition.fieldStart; f < Definition.fieldStart + Definition.field_count; f++)
                declaredFields.Add(new FieldInfo(pkg, f, this));

            // Add all methods
            declaredConstructors = new List<ConstructorInfo>();
            declaredMethods = new List<MethodInfo>();
            for (var m = Definition.methodStart; m < Definition.methodStart + Definition.method_count; m++) {
                var method = new MethodInfo(pkg, m, this);
                if (method.Name == ConstructorInfo.ConstructorName || method.Name == ConstructorInfo.TypeConstructorName)
                    declaredConstructors.Add(new ConstructorInfo(pkg, m, this));
                else
                    declaredMethods.Add(method);
            }

            // Add all properties
            declaredProperties = new List<PropertyInfo>();
            for (var p = Definition.propertyStart; p < Definition.propertyStart + Definition.property_count; p++)
                declaredProperties.Add(new PropertyInfo(pkg, p, this));

            // There are rare cases when explicitly implemented interface properties
            // are only given as methods in the metadata. Find these and add them as properties
            var eip = DeclaredMethods.Where(m => m.Name.Contains(".get_") || m.Name.Contains(".set_"))
                .Except(DeclaredProperties.Select(p => p.GetMethod))
                .Except(DeclaredProperties.Select(p => p.SetMethod));

            // Build a paired list of getters and setters
            var pairedEip = new List<(MethodInfo get, MethodInfo set)>();
            foreach (var p in eip) {
                // Discern property name
                var n = p.Name.Replace(".get_", ".").Replace(".set_", ".");

                // Find setter with no matching getter
                if (p.Name.Contains(".get_"))
                    if (pairedEip.FirstOrDefault(pe => pe.get == null && pe.set.Name == p.Name.Replace(".get_", ".set_")) is (MethodInfo get, MethodInfo set) method) {
                        pairedEip.Remove(method);
                        pairedEip.Add((p, method.set));
                    } else
                        pairedEip.Add((p, null));

                // Find getter with no matching setter
                if (p.Name.Contains(".set_"))
                    if (pairedEip.FirstOrDefault(pe => pe.set == null && pe.get.Name == p.Name.Replace(".set_", ".get_")) is (MethodInfo get, MethodInfo set) method) {
                        pairedEip.Remove(method);
                        pairedEip.Add((method.get, p));
                    } else
                        pairedEip.Add((null, p));
            }

            foreach (var prop in pairedEip)
                declaredProperties.Add(new PropertyInfo(prop.get, prop.set, this));

            // Add all events
            declaredEvents = new List<EventInfo>();
            for (var e = Definition.eventStart; e < Definition.eventStart + Definition.event_count; e++)
                declaredEvents.Add(new EventInfo(pkg, e, this));
        }

        // Initialize a type from a concrete generic instance
        private TypeInfo(TypeInfo genericTypeDef, TypeInfo[] genericArgs) : base(genericTypeDef.Assembly) {
            if (!genericTypeDef.IsGenericTypeDefinition)
                throw new InvalidOperationException(genericTypeDef.Name + " is not a generic type definition.");

            genericTypeDefinition = genericTypeDef;

            // Same visibility attributes as generic type definition
            Attributes = genericTypeDefinition.Attributes;

            // Even though this isn't a TypeDef, we have to set this so that DeclaringType works in later references
            Index = genericTypeDefinition.Index;

            // Same name as generic type definition
            Namespace = genericTypeDefinition.Namespace;
            Name = genericTypeDefinition.BaseName; // use BaseName to exclude the type parameters so we can supply our own
            MemberType = genericTypeDefinition.MemberType;

            IsGenericParameter = false;
            IsGenericType = true;

            genericArguments = genericArgs;
        }

        // Substitutes the elements of an array of types for the type parameters of the current generic type definition
        // and returns a TypeInfo object representing the resulting constructed type.
        // See: https://docs.microsoft.com/en-us/dotnet/api/system.type.makegenerictype?view=netframework-4.8
        public TypeInfo MakeGenericType(params TypeInfo[] typeArguments) {
            if (typeArguments.Length != genericArguments.Length) {
                throw new ArgumentException("The number of generic arguments provided does not match the generic type definition.");
            }

            TypeInfo result;
            if (genericTypeInstances.TryGetValue(typeArguments, out result))
                return result;
            result = new TypeInfo(this, typeArguments);
            genericTypeInstances[typeArguments] = result;
            return result;
        }

        public TypeInfo SubstituteGenericArguments(TypeInfo[] typeArguments, TypeInfo[] methodArguments = null) {
            if (!ContainsGenericParameters)
                return this;

            if (IsGenericTypeParameter)
                return typeArguments[GenericParameterPosition];
            else if (IsGenericMethodParameter)
                return methodArguments[GenericParameterPosition];
            else if (IsGenericTypeDefinition)
                return MakeGenericType(typeArguments);
            else if (HasElementType) {
                var elementType = ElementType.SubstituteGenericArguments(typeArguments, methodArguments);
                if (IsArray)
                    return elementType.MakeArrayType(GetArrayRank());
                else if (IsByRef)
                    return elementType.MakeByRefType();
                else if (IsPointer)
                    return elementType.MakePointerType();
                throw new InvalidOperationException("TypeInfo element type state is invalid!");
            } else {
                var newGenericArguments = genericArguments.Select(x => x.SubstituteGenericArguments(typeArguments, methodArguments));
                return genericTypeDefinition.MakeGenericType(newGenericArguments.ToArray());
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
            GenericParameterAttributes = (GenericParameterAttributes)param.flags;

            // Type constraints
            genericParameterConstraints = new TypeRef[param.constraintsCount];
            for (int c = 0; c < param.constraintsCount; c++)
                genericParameterConstraints[c] = TypeRef.FromReferenceIndex(Assembly.Model, Assembly.Model.Package.GenericConstraintIndices[param.constraintsStart + c]);

            // Base type of object (set by default)
            // TODO: ImplementedInterfaces should be set to interface types constraints

            // Name of parameter
            Name = Assembly.Model.Package.Strings[param.nameIndex];

            // Position
            GenericParameterPosition = param.num;

            IsGenericParameter = true;
            IsGenericType = false;
        }

        // Initialize a type that is a generic parameter of a generic method
        public TypeInfo(MethodBase declaringMethod, Il2CppGenericParameter param) : this(declaringMethod.DeclaringType, param)
            => DeclaringMethod = declaringMethod;

        // Initialize a type that is an array of the specified type
        private TypeInfo(TypeInfo elementType, int rank) : base(elementType.Assembly) {
            ElementType = elementType;
            IsArray = true;

            Namespace = ElementType.Namespace;
            Name = ElementType.Name;
            arrayRank = rank;
        }

        // Initialize a type that is a reference or pointer to the specified type
        private TypeInfo(TypeInfo underlyingType, bool isPointer) : base(underlyingType.Assembly) {
            ElementType = underlyingType;
            if (isPointer) {
                IsPointer = true;
            } else {
                IsByRef = true;
            }

            Namespace = ElementType.Namespace;
            Name = ElementType.Name;
        }

        public TypeInfo MakeArrayType(int rank = 1) {
            TypeInfo type;
            if (generatedArrayTypes.TryGetValue(rank, out type))
                return type;
            type = new TypeInfo(this, rank);
            generatedArrayTypes[rank] = type;
            return type;
        }

        public TypeInfo MakeByRefType() {
            if (generatedByRefType == null)
                generatedByRefType = new TypeInfo(this, isPointer: false);
            return generatedByRefType;
        }

        public TypeInfo MakePointerType() {
            if (generatedPointerType == null)
                generatedPointerType = new TypeInfo(this, isPointer: true);
            return generatedPointerType;
        }

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
            refs.UnionWith(DeclaredMethods.SelectMany(m => m.GetGenericArguments()));
            refs.UnionWith(DeclaredMethods.SelectMany(m => m.GetGenericArguments())
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

            // Generic type definition
            if (genericTypeDefinition != null)
                refs.Add(genericTypeDefinition);

            // Generic type parameters and constraints
            refs.UnionWith(GetGenericArguments());
            refs.UnionWith(GetGenericParameterConstraints());

            // Generic type constraints of type parameters in generic type definition
            refs.UnionWith(GenericTypeParameters.SelectMany(p => p.GetGenericParameterConstraints()));

            // Implemented interfaces
            refs.UnionWith(ImplementedInterfaces);

            // Repeatedly replace arrays, pointers and references with their element types
            while (refs.Any(r => r.HasElementType))
                refs = refs.Select(r => r.HasElementType ? r.ElementType : r).ToHashSet();

            // Type arguments in generic types that may have been a field, method parameter etc.
            IEnumerable<TypeInfo> genericArguments = refs.ToList();
            do {
                genericArguments = genericArguments.SelectMany(r => r.GetGenericArguments());
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

        public string GetAccessModifierString() => this switch
        {
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
            if (DeclaringMethod == null && DeclaringType.IsNested && DeclaringType.DeclaringType.GetGenericArguments().Any(p => p.Name == Name))
                return string.Empty;

            // Check if we are in an overriding method, and if so, exclude ourselves if we are a generic type parameter from the base method
            // All constraints are inherited automatically by all overriding methods so we only have to look at the immediate base method
            if (DeclaringMethod != null && DeclaringMethod.IsVirtual && !DeclaringMethod.IsAbstract && !DeclaringMethod.IsFinal
                && (DeclaringMethod.Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.ReuseSlot) {
                // Find nearest ancestor base method which has us as a generic type parameter
                var method = DeclaringMethod.DeclaringType.BaseType.GetAllMethods()
                    .FirstOrDefault(m => m.IsHideBySig && m.IsVirtual && DeclaringMethod.SignatureEquals(m) && m.GetGenericArguments().Any(p => p.Name == Name));

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