# Shared interface
def AsUTF8(s):
	return s if sys.version_info[0] > 2 else s.encode('utf-8')

def ParseAddress(d):
	return int(d['virtualAddress'], 0)

def DefineILMethod(jsonDef):
	addr = ParseAddress(jsonDef)
	SetName(addr, AsUTF8(jsonDef['name']))
	SetFunctionType(addr, AsUTF8(jsonDef['signature']))
	SetHeaderComment(addr, AsUTF8(jsonDef['dotNetSignature']))

def DefineILMethodInfo(jsonDef):
	addr = ParseAddress(jsonDef)
	SetName(addr, AsUTF8(jsonDef['name']))
	SetType(addr, r'struct MethodInfo *')
	SetComment(addr, AsUTF8(jsonDef['dotNetSignature']))

def DefineCppFunction(jsonDef):
	addr = ParseAddress(jsonDef)
	SetName(addr, AsUTF8(jsonDef['name']))
	SetFunctionType(addr, AsUTF8(jsonDef['signature']))

def DefineString(jsonDef):
	addr = ParseAddress(jsonDef)
	SetName(addr, AsUTF8(jsonDef['name']))
	SetType(addr, r'struct String *')
	SetComment(addr, AsUTF8(jsonDef['string']))

def DefineFieldFromJson(jsonDef):
	DefineField(jsonDef['virtualAddress'], jsonDef['name'], jsonDef['type'], jsonDef['dotNetType'])

def DefineField(addr, name, type, ilType = None):
	addr = int(addr, 0)
	SetName(addr, AsUTF8(name))
	SetType(addr, AsUTF8(type))
	if (ilType is not None):
		SetComment(addr, AsUTF8(ilType))

def DefineArray(jsonDef):
	addr = ParseAddress(jsonDef)
	MakeArray(addr, int(jsonDef['count']), AsUTF8(jsonDef['type']))
	SetName(addr, AsUTF8(jsonDef['name']))

# Process JSON
def ProcessJSON(jsonData):

	# Method definitions
	print('Processing method definitions')
	for d in jsonData['methodDefinitions']:
		DefineILMethod(d)
	
	# Constructed generic methods
	print('Processing constructed generic methods')
	for d in jsonData['constructedGenericMethods']:
		DefineILMethod(d)

	# Custom attributes generators
	print('Processing custom attributes generators')
	for d in jsonData['customAttributesGenerators']:
		DefineCppFunction(d)
	
	# Method.Invoke thunks
	print('Processing Method.Invoke thunks')
	for d in jsonData['methodInvokers']:
		DefineCppFunction(d)

	# String literals for version >= 19
	print('Processing string literals')
	if 'virtualAddress' in jsonData['stringLiterals'][0]:
		for d in jsonData['stringLiterals']:
			DefineString(d)

	# String literals for version < 19
	else:
		litDecl = 'enum StringLiteralIndex {\n'
		for d in jsonData['stringLiterals']:
			litDecl += "  " + AsUTF8(d['name']) + ",\n"
		litDecl += '};\n'
		DefineCode(litDecl)
	
	# Il2CppClass (TypeInfo) pointers
	print('Processing Il2CppClass (TypeInfo) pointers')
	for d in jsonData['typeInfoPointers']:
		DefineFieldFromJson(d)
	
	# Il2CppType (TypeRef) pointers
	print('Processing Il2CppType (TypeRef) pointers')
	for d in jsonData['typeRefPointers']:
		DefineField(d['virtualAddress'], d['name'], r'struct Il2CppType *', d['dotNetType'])
	
	# MethodInfo pointers
	print('Processing MethodInfo pointers')
	for d in jsonData['methodInfoPointers']:
		DefineILMethodInfo(d)

	# Function boundaries
	print('Processing function boundaries')
	functionAddresses = jsonData['functionAddresses']
	functionAddresses.sort()
	count = len(functionAddresses)
	for i in range(count):
		addrStart = int(functionAddresses[i],0)
		if addrStart == 0:
			continue
		addrNext = None
		if i != count -1:
			addrNext = int(functionAddresses[i+1],0)
		MakeFunction(addrStart,None,addrNext)

	# IL2CPP type metadata
	print('Processing IL2CPP type metadata')
	for d in jsonData['typeMetadata']:
		DefineField(d['virtualAddress'], d['name'], d['type'])
	
	# IL2CPP function metadata
	print('Processing IL2CPP function metadata')
	for d in jsonData['functionMetadata']:
		DefineCppFunction(d)

	# IL2CPP array metadata
	print('Processing IL2CPP array metadata')
	for d in jsonData['arrayMetadata']:
		DefineArray(d)

	# IL2CPP API functions
	print('Processing IL2CPP API functions')
	for d in jsonData['apis']:
		DefineCppFunction(d)

# Entry point
print('Generated script file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty')
CustomInitializer()

with open(os.path.join(GetScriptDirectory(), "%JSON_METADATA_RELATIVE_PATH%"), "r") as jsonFile:
	jsonData = json.load(jsonFile)['addressMap']
	ProcessJSON(jsonData)

print('Script execution complete.')
