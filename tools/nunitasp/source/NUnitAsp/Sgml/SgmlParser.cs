using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Net;
using System.Xml;

namespace Sgml
{
    public enum LiteralType 
    {
        CDATA, SDATA, PI
    };

    public class Entity {
        public const Char EOF = (char)65535;
        public string Proxy;

        public Entity(string name, string pubid, string uri, string proxy) {
            Name = name;
            PublicId = pubid;
            Uri = uri;
            Proxy = proxy;
            Html = (name != null && name.ToLower() == "html");
        }

        public Entity(string name, string literal) {
            Name = name;
            Literal = literal;
            Internal = true;
        }

        public Entity(string name, Uri baseUri, TextReader stm, string proxy) {
            Name = name;
            Internal = true;
            _stm = stm;
            _resolvedUri = baseUri;
            Proxy = proxy;
            Html = (name.ToLower() == "html");
        }

        public string Name;
        public bool Internal;
        public string PublicId;
        public string Uri;
        public string Literal;
        public LiteralType LiteralType;
        public Entity Parent;
        public bool Html;

        public Uri ResolvedUri {
            get {
                if (_resolvedUri != null) return _resolvedUri;
                else if (Parent != null) return Parent.ResolvedUri;
                return null;
            }
        }

        Uri _resolvedUri;
        TextReader _stm;
        bool _weOwnTheStream;

        public int Line;
        int _LineStart;
        int _absolutePos;
        public char Lastchar;
        public bool IsWhitespace;

        public int LinePosition {
            get { return _absolutePos - _LineStart + 1; }
        }

        public char ReadChar() {
            char ch = (char)_stm.Read();
            if (ch == 0) {
                // convert nulls to whitespace, since they are not valid in XML anyway.
                ch = ' ';
            }
            _absolutePos++;
            if (ch == 0xa) {
                IsWhitespace = true;
                _LineStart = _absolutePos+1;
                Line++;
            } 
            else if (ch == ' ' || ch == '\t') {
                IsWhitespace = true;
                if (Lastchar == 0xd) {
                    _LineStart = _absolutePos;
                    Line++;
                }
            }
            else if (ch == 0xd) {
                IsWhitespace = true;
            }
            else {
                IsWhitespace = false;
                if (Lastchar == 0xd) {
                    Line++;
                    _LineStart = _absolutePos;
                }
            } 
            Lastchar = ch;
            return ch;
        }

        public void Open(Entity parent, Uri baseUri) {
            Parent = parent;
            if (parent != null) this.Html = parent.Html;
            this.Line = 1;
            if (Internal) {
                if (this.Literal != null)
                    _stm = new StringReader(this.Literal);
            } 
            else if (this.Uri == null) {
                this.Error("Unresolvable entity '{0}'", this.Name);
            }
            else {
                if (baseUri != null) {
                    // bugbug: new Uri(baseUri, this.Uri) when baseUri is
                    // file://currentdirectory and this.Uri is "\temp\test.htm"
                    // resolves to a UNC with LocalPath starting with \\ which
                    // is wrong!
                    if (baseUri.Scheme == "file") {
                        // bugbug: Path.Combine looses the base path's drive name!
                        string path = baseUri.LocalPath;
                        int i = path.IndexOf(":");
                        string drive = "";
                        if(i>0) {
                            drive = path.Substring(0,i+1);
                        }
                        string s = Path.Combine(baseUri.LocalPath, this.Uri);
                        if (s.Substring(1,2) == ":\\") drive = "";
                        string uri = "file:///"+drive+s;
                        _resolvedUri = new Uri(uri, true);
                    } 
                    else {
                        _resolvedUri = new Uri(baseUri, this.Uri);              
                    }
                }
                else {
                    _resolvedUri = new Uri(this.Uri);
                }
                switch (_resolvedUri.Scheme) {
                    case "file": {
                        string path = _resolvedUri.LocalPath;
                        _stm = new StreamReader(
                            new FileStream(path, FileMode.Open, FileAccess.Read),
                            Encoding.Default, true);
                        _weOwnTheStream = true;
                    }
                        break;
                    default:
                        //Console.WriteLine("Fetching:" + ResolvedUri.AbsoluteUri);
                        HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(ResolvedUri);
                        wr.Timeout = 10000; // in case this is running in an ASPX page.
                        if (Proxy != null) wr.Proxy = new WebProxy(Proxy);
                        wr.PreAuthenticate = false; 
                        // Pass the credentials of the process. 
                        wr.Credentials = CredentialCache.DefaultCredentials; 

                        WebResponse resp = wr.GetResponse();
                        Uri actual = resp.ResponseUri;
                        if (actual.AbsoluteUri != _resolvedUri.AbsoluteUri) {
                            _resolvedUri = actual;
                        }                       
                        string contentType = resp.ContentType.ToLower();
                        int i = contentType.IndexOf("charset");
                        Encoding e = Encoding.Default;
                        if (i >= 0)
                        {
                            int j = contentType.IndexOf("=", i);
                            int k = contentType.IndexOf(";", j);
                            if (k<0) k = contentType.Length;
                            if (j > 0)
                            {
                              j++;
                              string charset = contentType.Substring(j, k - j).Trim();
                              try
                              {
                                e = Encoding.GetEncoding(charset);
                              } catch (Exception)
                              {
                              }
                            }
                        }
                        _stm = new StreamReader(resp.GetResponseStream(),
                            e, true);
                        _weOwnTheStream = true;
                        break;

                }
            }
        }

        public void Close() {
            if (_weOwnTheStream) 
                _stm.Close();
        }

        public char SkipWhitespace() {
            char ch = Lastchar;
            while (ch != Entity.EOF && (ch == ' ' || ch == '\r' || ch == '\n' || ch == '\t')) {
                ch = ReadChar();
            }
            return ch;
        }

        public string ScanToken(StringBuilder sb, string term, bool nmtoken) {
            sb.Length = 0;
            char ch = Lastchar;
            if (nmtoken && ch != '_' && !Char.IsLetter(ch)) {
                throw new Exception(
                    String.Format("Invalid name start character '{0}'", ch));
            }
            while (ch != Entity.EOF && term.IndexOf(ch)<0) {
                if (!nmtoken || ch == '_' || ch == '.' || ch == '-' || ch == ':' || Char.IsLetterOrDigit(ch)) {
                    sb.Append(ch);
                } 
                else {
                    throw new Exception(
                        String.Format("Invalid name character '{0}'", ch));
                }
                ch = ReadChar();
            }
            return sb.ToString();
        }

        public string ScanLiteral(StringBuilder sb, char quote) {
            sb.Length = 0;
            char ch = ReadChar();
            while (ch != Entity.EOF && ch != quote ) {
                if (ch == '&') {
                    ch = ReadChar();
                    if (ch == '#') {
                        string charent = ExpandCharEntity();
                        sb.Append(charent);
                    } 
                    else {
                        sb.Append('&');
                        sb.Append(ch);
                    }
                }               
                else {
                    sb.Append(ch);
                }
                ch = ReadChar();
            }
            ReadChar(); // consume end quote.           
            return sb.ToString();
        }

        public string ScanToEnd(StringBuilder sb, string type, string terminators) {
            if (sb != null) sb.Length = 0;
            int start = Line;
            // This method scans over a chunk of text looking for the
            // termination sequence specified by the 'terminators' parameter.
            char ch = ReadChar();            
            int state = 0;
            char next = terminators[state];
            while (ch != Entity.EOF) {
                if (Char.ToLower(ch) == Char.ToLower(next)) {
                    state++;
                    if (state >= terminators.Length) {
                        // found it!
                        break;
                    }
                    next = terminators[state];
                } 
                else if (state > 0) {
                    // char didn't match, so go back and see how much does still match.
                    int i = state - 1;
                    int newstate = 0;
                    while (i>=0 && newstate==0) {
                        if (terminators[i] == ch) {
                            // character is part of the terminators pattern, ok, so see if we can
                            // match all the way back to the beginning of the pattern.
                            int j = 1;
                            while( i-j>=0) {
                                if (terminators[i-j] != terminators[state-j])
                                    break;
                                j++;
                            }
                            if (j>i) {
                                newstate = i+1;
                            }
                        } 
                        else {
                            i--;
                        }
                    }
                    if (sb != null) {
                        i = (i<0) ? 1 : 0;
                        for (int k = 0; k <= state-newstate-i; k++) {
                            sb.Append(terminators[k]); 
                        }
                        if (i>0) // see if we've matched this char or not
                            sb.Append(ch); // if not then append it to buffer.
                    }
                    state = newstate;
                    next = terminators[newstate];
                }
                else {
                    if (sb != null) sb.Append(ch);
                }
                ch = ReadChar();
            }
            if (ch == 0) Error(type + " starting on line {0} was never closed", start);
            ReadChar(); // consume last char in termination sequence.
            if (sb != null) return sb.ToString();
            return "";
        }

        public string ExpandCharEntity() {
            char ch = ReadChar();
            int v = 0;
            if (ch == 'x') {
                for (; ch != Entity.EOF && ch != ';'; ch = ReadChar()) {
                    int p = 0;
                    if (ch >= '0' && ch <= '9') {
                        p = (int)(ch-'0');
                    } 
                    else if (ch >= 'a' && ch <= 'f') {
                        p = (int)(ch-'a')+10;
                    } 
                    else if (ch >= 'A' && ch <= 'F') {
                        p = (int)(ch-'A')+10;
                    }
                    else {
                        break;//we must be done!
                        //Error("Hex digit out of range '{0}'", (int)ch);
                    }
                    v = (v*16)+p;
                }
            } 
            else {                   
                for (; ch != Entity.EOF && ch != ';'; ch = ReadChar()) {
                    if (ch >= '0' && ch <= '9') {
                        v = (v*10)+(int)(ch-'0');
                    } 
                    else {
                        break; // we must be done!
                        //Error("Decimal digit out of range '{0}'", (int)ch);
                    }
                }
            }
            if (ch == 0) {
                Error("Premature {0} parsing entity reference", ch); 
            }
            // HACK ALERT: IE and Netscape map the unicode characters 
            if (v >= 0x80 & v <= 0x9F && this.Html) {
                // This range of control characters is mapped to Windows-1252!
                int size = CtrlMap.Length;
                int i = v-0x80;
                int unicode = CtrlMap[i];
                return Convert.ToChar(unicode).ToString();
            }
            return Convert.ToChar(v).ToString();
        }

        static int[] CtrlMap = new int[] {
            // This is the windows-1252 mapping of the code points 0x80 through 0x9f.
            8364, 129, 8218, 402, 8222, 8230, 8224, 8225, 710, 8240, 352, 8249, 338, 141,
            381, 143, 144, 8216, 8217, 8220, 8221, 8226, 8211, 8212, 732, 8482, 353, 8250, 
            339, 157, 382, 376
        };

        public void Error(string msg)
        {
            throw new Exception(msg);
        }

        public void Error(string msg, char ch)
        {
            string str = (ch == Entity.EOF) ? "EOF" : Char.ToString(ch);            
            throw new Exception(String.Format(msg, str));
        }

        public void Error(string msg, int x)
        {
            throw new Exception(String.Format(msg, x));
        }

        public void Error(string msg, string arg)
        {
            throw new Exception(String.Format(msg, arg));
        }

        public string Context()
        {
            Entity p = this;
            StringBuilder sb = new StringBuilder();
            while (p != null) 
            {
                string msg;
                if (p.Internal) 
                {
                    msg = String.Format("\nReferenced on line {0}, position {1} of internal entity '{2}'", p.Line, p.LinePosition, p.Name);
                } 
                else 
                {
                    msg = String.Format("\nReferenced on line {0}, position {1} of '{2}' entity at [{3}]", p.Line, p.LinePosition, p.Name, p.ResolvedUri.AbsolutePath);
                }
                sb.Append(msg);
                p = p.Parent;
            }
            return sb.ToString();
        }

        public static bool IsLiteralType(string token)
        {
            return (token == "CDATA" || token == "SDATA" || token == "PI");
        }

        public void SetLiteralType(string token)
        {
            switch (token) 
            {
                case "CDATA":
                    LiteralType = LiteralType.CDATA;
                    break;
                case "SDATA":
                    LiteralType = LiteralType.SDATA;
                    break;
                case "PI":
                    LiteralType = LiteralType.PI;
                    break;
            }
        }
    }

    public class ElementDecl
    {
        public ElementDecl(string name, bool sto, bool eto, ContentModel cm, string[] inclusions, string[] exclusions)
        {
            Name = name;
            StartTagOptional = sto;
            EndTagOptional = eto;
            ContentModel = cm;
            Inclusions = inclusions;
            Exclusions = exclusions;
        }
        public string Name;
        public bool StartTagOptional;
        public bool EndTagOptional;
        public ContentModel ContentModel;
        public string[] Inclusions;
        public string[] Exclusions;

        public AttList AttList;

        public void AddAttDefs(AttList list)
        {
            if (AttList == null) 
            {
                AttList = list;
            } 
            else 
            {               
                foreach (AttDef a in list) 
                {
                    if (AttList[a.Name] == null) 
                    {
                        AttList.Add(a);
                    }
                }
            }
        }

        public bool CanContain(string name, SgmlDtd dtd)
        {
            // return true if this element is allowed to contain the given element.
            if (Exclusions != null) 
            {
                foreach (string s in Exclusions) 
                {
                    if ((object)s == (object)name) // XmlNameTable optimization
                        return false;
                }
            }
            if (Inclusions != null) 
            {
                foreach (string s in Inclusions) 
                {
                    if ((object)s == (object)name) // XmlNameTable optimization
                        return true;
                }
            }
            return ContentModel.CanContain(name, dtd);
        }
    }

    public enum DeclaredContent
    {
        Default, CDATA, RCDATA, EMPTY
    }

    public class ContentModel
    {
        public DeclaredContent DeclaredContent;
        public int CurrentDepth;
        public Group Model;

        public ContentModel()
        {
            Model = new Group(null);
        }

        public void PushGroup()
        {
            Model = new Group(Model);
            CurrentDepth++;
        }

        public int PopGroup()
        {
            if (CurrentDepth == 0) return -1;
            CurrentDepth--;
            Model.Parent.AddGroup(Model);
            Model = Model.Parent;
            return CurrentDepth;
        }

        public void AddSymbol(string sym)
        {
            Model.AddSymbol(sym);
        }

        public void AddConnector(char c)
        {
            Model.AddConnector(c);
        }

        public void AddOccurrence(char c)
        {
            Model.AddOccurrence(c);
        }

        public void SetDeclaredContent(string dc)
        {
            switch (dc) {
                case "EMPTY":
                    this.DeclaredContent = DeclaredContent.EMPTY;
                    break;
                case "RCDATA":
                    this.DeclaredContent = DeclaredContent.RCDATA;
                    break;
                case "CDATA":
                    this.DeclaredContent = DeclaredContent.CDATA;
                    break;
                default:
                    throw new Exception(
                        String.Format("Declared content type '{0}' is not supported", dc));
            }
        }

        public bool CanContain(string name, SgmlDtd dtd)
        {
            if (DeclaredContent != DeclaredContent.Default)
                return false; // empty or text only node.
            return Model.CanContain(name, dtd);
        }
    }

    public enum GroupType 
    {
        None, And, Or, Sequence 
    };

    public enum Occurrence
    {
        Required, Optional, ZeroOrMore, OneOrMore
    }

    public class Group
    {
        public Group Parent;
        public ArrayList Members;
        public GroupType GroupType;
        public Occurrence Occurrence;
        public bool Mixed;

        public Group(Group parent)
        {
            Parent = parent;
            Members = new ArrayList();
            this.GroupType = GroupType.None;
            Occurrence = Occurrence.Required;
        }
        public void AddGroup(Group g)
        {
            Members.Add(g);
        }
        public void AddSymbol(string sym)
        {
            if (sym == "#PCDATA") 
            {               
                Mixed = true;
            } 
            else 
            {
                Members.Add(sym);
            }
        }
        public void AddConnector(char c)
        {
            if (!Mixed && Members.Count == 0) 
            {
                throw new Exception(
                    String.Format("Missing token before connector '{0}'.", c)
                    );
            }
            GroupType gt = GroupType.None;
            switch (c) 
            {
                case ',': 
                    gt = GroupType.Sequence;
                    break;
                case '|':
                    gt = GroupType.Or;
                    break;
                case '&':
                    gt = GroupType.And;
                    break;
            }
            if (GroupType != GroupType.None && GroupType != gt) 
            {
                throw new Exception(
                    String.Format("Connector '{0}' is inconsistent with {1} group.", c, GroupType.ToString())
                    );
            }
            GroupType = gt;
        }

        public void AddOccurrence(char c)
        {
            Occurrence o = Occurrence.Required;
            switch (c) 
            {
                case '?': 
                    o = Occurrence.Optional;
                    break;
                case '+':
                    o = Occurrence.OneOrMore;
                    break;
                case '*':
                    o = Occurrence.ZeroOrMore;
                    break;
            }
            Occurrence = o;
        }

        // Rough approximation - this is really assuming an "Or" group
        public bool CanContain(string name, SgmlDtd dtd)
        {
            // Do a simple search of members.
            foreach (object obj in Members) 
            {
                if (obj is String) 
                {
                    if (obj == (object)name) // XmlNameTable optimization
                        return true;
                } 
            }
            // didn't find it, so do a more expensive search over child elements
            // that have optional start tags and over child groups.
            foreach (object obj in Members) 
            {
                if (obj is String) 
                {
                    string s = (string)obj;
                    ElementDecl e = dtd.FindElement(s);
                    if (e != null) 
                    {
                        if (e.StartTagOptional) 
                        {
                            // tricky case, the start tag is optional so element may be
                            // allowed inside this guy!
                            if (e.CanContain(name, dtd))
                                return true;
                        }
                    }
                } 
                else 
                {
                    Group m = (Group)obj;
                    if (m.CanContain(name, dtd)) 
                        return true;
                }
            }
            return false;
        }
    }

    public enum AttributeType
    {
        DEFAULT, CDATA, ENTITY, ENTITIES, ID, IDREF, IDREFS, NAME, NAMES, NMTOKEN, NMTOKENS, 
        NUMBER, NUMBERS, NUTOKEN, NUTOKENS, NOTATION, ENUMERATION
    }

    public enum AttributePresence
    {
        DEFAULT, FIXED, REQUIRED, IMPLIED
    }

    public class AttDef
    {
        public string Name;
        public AttributeType Type;
        public string[] EnumValues;
        public string Default;
        public AttributePresence Presence;

        public AttDef(string name)
        {
            Name = name;
        }


        public void SetType(string type)
        {
            switch (type) 
            {
                case "CDATA":
                    Type = AttributeType.CDATA;
                    break;
                case "ENTITY":
                    Type = AttributeType.ENTITY;
                    break;
                case "ENTITIES":
                    Type = AttributeType.ENTITIES;
                    break;
                case "ID":
                    Type = AttributeType.ID;
                    break;
                case "IDREF":
                    Type = AttributeType.IDREF;
                    break;
                case "IDREFS":
                    Type = AttributeType.IDREFS;
                    break;
                case "NAME":
                    Type = AttributeType.NAME;
                    break;
                case "NAMES":
                    Type = AttributeType.NAMES;
                    break;
                case "NMTOKEN":
                    Type = AttributeType.NMTOKEN;
                    break;
                case "NMTOKENS":
                    Type = AttributeType.NMTOKENS;
                    break;
                case "NUMBER":
                    Type = AttributeType.NUMBER;
                    break;
                case "NUMBERS":
                    Type = AttributeType.NUMBERS;
                    break;
                case "NUTOKEN":
                    Type = AttributeType.NUTOKEN;
                    break;
                case "NUTOKENS":
                    Type = AttributeType.NUTOKENS;
                    break;
                default:
                    throw new Exception("Attribute type '"+type+"' is not supported");
            }
        }

        public bool SetPresence (string token)
        {
            bool hasDefault = true;
            if (token == "FIXED") 
            {
                Presence = AttributePresence.FIXED;             
            } 
            else if (token == "REQUIRED") 
            {
                Presence = AttributePresence.REQUIRED;
                hasDefault = false;
            }
            else if (token == "IMPLIED") 
            {
                Presence = AttributePresence.IMPLIED;
                hasDefault = false;
            }
            else 
            {
                throw new Exception(String.Format("Attribute value '{0}' not supported", token));
            }
            return hasDefault;
        }
    }

    public class AttList : IEnumerable
    {
        Hashtable AttDefs;
        
        public AttList()
        {
            AttDefs = new Hashtable();
        }

        public void Add(AttDef a)
        {
            AttDefs.Add(a.Name, a);
        }

        public AttDef this[string name]
        {
            get 
            {
                return (AttDef)AttDefs[name];
            }
        }

        public IEnumerator GetEnumerator()
        {
            return AttDefs.Values.GetEnumerator();
        }
    }

    public class SgmlDtd
    {
        public string Name;

        Hashtable _elements;
        Hashtable _pentities;
        Hashtable _entities;
        StringBuilder _sb;      
        Entity _current;
        XmlNameTable _nt;

        public SgmlDtd(string name, XmlNameTable nt)
        {
            _nt = nt;
            if (name != null) Name = _nt.Add(name.ToLower());
            _elements = new Hashtable();
            _pentities = new Hashtable();
            _entities = new Hashtable();
            _sb = new StringBuilder();
        }

        public XmlNameTable NameTable { get { return _nt; } }

        public static SgmlDtd Parse(Uri baseUri, string name, string pubid, string url, string subset, string proxy, XmlNameTable nt)
        {
            SgmlDtd dtd = new SgmlDtd(name, nt);
            if (url != null && url != "") 
            {
                dtd.PushEntity(baseUri, new Entity(dtd.Name, pubid, url, proxy));
            }
            if (subset != null && subset != "") 
            {
                dtd.PushEntity(baseUri, new Entity(name, subset));
            }
            try 
            {
                dtd.Parse();
            } 
            catch (Exception e)
            {
                throw new Exception(e.Message + dtd._current.Context());
            }           
            return dtd;
        }
        public static SgmlDtd Parse(Uri baseUri, string name, string pubid, TextReader input, string subset, string proxy, XmlNameTable nt) {
            SgmlDtd dtd = new SgmlDtd(name, nt);
            dtd.PushEntity(baseUri, new Entity(dtd.Name, baseUri, input, proxy));
            if (subset != null && subset != "") {
                dtd.PushEntity(baseUri, new Entity(name, subset));
            }
            try {
                dtd.Parse();
            } 
            catch (Exception e) {
                throw new Exception(e.Message + dtd._current.Context());
            }           
            return dtd;
        }

        public Entity FindEntity(string name)
        {
            return (Entity)_entities[name];
        }

        public ElementDecl FindElement(string name)
        {
            return (ElementDecl)_elements[name];
        }

        //-------------------------------- Parser -------------------------
        void PushEntity(Uri baseUri, Entity e)
        {
            e.Open(_current, baseUri);
            _current = e;
            _current.ReadChar();
        }

        void PopEntity()
        {
            if (_current != null) _current.Close();
            if (_current.Parent != null) 
            {
                _current = _current.Parent;
            } 
            else 
            {
                _current = null;
            }
        }

        void Parse()
        {
            char ch = _current.Lastchar;
            while (true) 
            {
                switch (ch) 
                {
                    case Entity.EOF:
                        PopEntity();
                        if (_current == null)
                            return;
                        ch = _current.Lastchar;
                        break;
                    case ' ':
                    case '\n':
                    case '\r':
                    case '\t':
                        ch = _current.ReadChar();
                        break;
                    case '<':
                        ParseMarkup();
                        ch = _current.ReadChar();
                        break;
                    case '%':
                        Entity e = ParseParameterEntity(_ws);
                        try 
                        {
                            PushEntity(_current.ResolvedUri, e);
                        } 
                        catch (Exception ex) 
                        {
                            // bugbug - need an error log.
                            Console.WriteLine(ex.Message + _current.Context());
                        }
                        ch = _current.Lastchar;
                        break;
                    default:
                        _current.Error("Unexpected character '{0}'", ch);
                        break;
                }               
            }
        }

        void ParseMarkup()
        {
            char ch = _current.ReadChar();
            if (ch != '!') 
            {
                _current.Error("Found '{0}', but expecing declaration starting with '<!'");
                return;
            }
            ch = _current.ReadChar();
            if (ch == '-') 
            {
                ch = _current.ReadChar();
                if (ch != '-') _current.Error("Expecting comment '<!--' but found {0}", ch);
                _current.ScanToEnd(_sb, "Comment", "-->");
            } 
            else if (ch == '[') 
            {
                ParseMarkedSection();
            }
            else 
            {
                string token = _current.ScanToken(_sb, _ws, true);
                switch (token) 
                {
                    case "ENTITY":
                        ParseEntity();
                        break;
                    case "ELEMENT":
                        ParseElementDecl();
                        break;
                    case "ATTLIST":
                        ParseAttList();
                        break;
                    default:
                        _current.Error("Invalid declaration '<!{0}'.  Expecting 'ENTITY', 'ELEMENT' or 'ATTLIST'.", token);
                        break;
                }
            }
        }

        char ParseDeclComments()
        {
            char ch = _current.Lastchar;
            while (ch == '-') 
            {
                ch = ParseDeclComment(true);
            }
            return ch;
        }

        char ParseDeclComment(bool full)
        {
            int start = _current.Line;
            // -^-...--
            // This method scans over a comment inside a markup declaration.
            char ch = _current.ReadChar();
            if (full && ch != '-') _current.Error("Expecting comment delimiter '--' but found {0}", ch);
            _current.ScanToEnd(_sb, "Markup Comment", "--");
            return _current.SkipWhitespace();
        }

        void ParseMarkedSection()
        {
            // <![^ name [ ... ]]>
            _current.ReadChar(); // move to next char.
            string name = ScanName("[");
            if (name == "INCLUDE") 
            {
                ParseIncludeSection();
            } 
            else if (name == "IGNORE") 
            {
                ParseIgnoreSection();
            }
            else 
            {
                _current.Error("Unsupported marked section type '{0}'", name);
            }
        }

        void ParseIncludeSection()
        {
            throw new NotImplementedException("Include Section");
        }

        void ParseIgnoreSection()
        {
            int start = _current.Line;
            // <!-^-...-->
            char ch = _current.SkipWhitespace();
            if (ch != '[') _current.Error("Expecting '[' but found {0}", ch);
            _current.ScanToEnd(_sb, "Conditional Section", "]]>");
        }

        string ScanName(string term)
        {
            // skip whitespace, scan name (which may be parameter entity reference
            // which is then expanded to a name)
            char ch = _current.SkipWhitespace();
            if (ch == '%') 
            {
                Entity e = ParseParameterEntity(term);
                ch = _current.Lastchar;
                // bugbug - need to support external and nested parameter entities
                if (!e.Internal) throw new NotSupportedException("External parameter entity resolution");
                return e.Literal.Trim();
            } 
            else 
            {
                return _current.ScanToken(_sb, term, true);
            }
        }

        Entity ParseParameterEntity(string term)
        {
            // almost the same as _current.ScanToken, except we also terminate on ';'
            char ch = _current.ReadChar();
            string name =  _current.ScanToken(_sb, ";"+term, false);
            name = _nt.Add(name);
            if (_current.Lastchar == ';') 
                _current.ReadChar();
            Entity e = GetParameterEntity(name);
            return e;
        }

        Entity GetParameterEntity(string name)
        {
            Entity e = (Entity)_pentities[name];
            if (e == null) _current.Error("Reference to undefined parameter entity '{0}'", name);
            return e;
        }
        
        static string _ws = " \r\n\t";

        void ParseEntity()
        {
            char ch = _current.SkipWhitespace();
            bool pe = (ch == '%');
            if (pe)
            {
                // parameter entity.
                _current.ReadChar(); // move to next char
                ch = _current.SkipWhitespace();
            }
            string name = _current.ScanToken(_sb, _ws, true);
            name = _nt.Add(name);
            ch = _current.SkipWhitespace();
            Entity e = null;
            if (ch == '"' || ch == '\'') 
            {
                string literal = _current.ScanLiteral(_sb, ch);
                e = new Entity(name, literal);                
            } 
            else 
            {
                string pubid = null;
                string extid = null;
                string tok = _current.ScanToken(_sb, _ws, true);
                if (Entity.IsLiteralType(tok) )
                {
                    ch = _current.SkipWhitespace();
                    string literal = _current.ScanLiteral(_sb, ch);
                    e = new Entity(name, literal);
                    e.SetLiteralType(tok);
                }
                else 
                {
                    extid = tok;
                    if (extid == "PUBLIC") 
                    {
                        ch = _current.SkipWhitespace();
                        if (ch == '"' || ch == '\'') 
                        {
                            pubid = _current.ScanLiteral(_sb, ch);
                        } 
                        else 
                        {
                            _current.Error("Expecting public identifier literal but found '{0}'",ch);
                        }
                    } 
                    else if (extid != "SYSTEM") 
                    {
                        _current.Error("Invalid external identifier '{0}'.  Expecing 'PUBLIC' or 'SYSTEM'.", extid);
                    }
                    string uri = null;
                    ch = _current.SkipWhitespace();
                    if (ch == '"' || ch == '\'') 
                    {
                        uri = _current.ScanLiteral(_sb, ch);
                    } 
                    else if (ch != '>')
                    {
                        _current.Error("Expecting system identifier literal but found '{0}'",ch);
                    }
                    e = new Entity(name, pubid, uri, _current.Proxy);
                }
            }
            ch = _current.SkipWhitespace();
            if (ch == '-') 
                ch = ParseDeclComments();
            if (ch != '>') 
            {
                _current.Error("Expecting end of entity declaration '>' but found '{0}'", ch);  
            }           
            if (pe) _pentities.Add(e.Name, e);
            else _entities.Add(e.Name, e);
        }

        void ParseElementDecl()
        {
            char ch = _current.SkipWhitespace();
            string[] names = ParseNameGroup(ch, true);
            bool sto = (_current.SkipWhitespace() == 'O'); // start tag optional?   
            _current.ReadChar();
            bool eto = (_current.SkipWhitespace() == 'O'); // end tag optional? 
            _current.ReadChar();
            ch = _current.SkipWhitespace();
            ContentModel cm = ParseContentModel(ch);
            ch = _current.SkipWhitespace();

            string [] exclusions = null;
            string [] inclusions = null;

            if (ch == '-') 
            {
                ch = _current.ReadChar();
                if (ch == '(') 
                {
                    exclusions = ParseNameGroup(ch, true);
                    ch = _current.SkipWhitespace();
                }
                else if (ch == '-') 
                {
                    ch = ParseDeclComment(false);
                } 
                else 
                {
                    _current.Error("Invalid syntax at '{0}'", ch);  
                }
            }

            if (ch == '-') 
                ch = ParseDeclComments();

            if (ch == '+') 
            {
                ch = _current.ReadChar();
                if (ch != '(') 
                {
                    _current.Error("Expecting inclusions name group", ch);  
                }
                inclusions = ParseNameGroup(ch, true);
                ch = _current.SkipWhitespace();
            }

            if (ch == '-') 
                ch = ParseDeclComments();


            if (ch != '>') 
            {
                _current.Error("Expecting end of ELEMENT declaration '>' but found '{0}'", ch); 
            }

            foreach (string name in names) 
            {
                string atom = _nt.Add(name.ToLower()); 
                _elements.Add(atom, new ElementDecl(atom, sto, eto, cm, inclusions, exclusions));
            }
        }

        static string _ngterm = " \r\n\t|)";
        string[] ParseNameGroup(char ch, bool nmtokens)
        {
            ArrayList names = new ArrayList();
            if (ch == '(') 
            {
                ch = _current.ReadChar();
                ch = _current.SkipWhitespace();
                while (ch != ')') 
                {
                    // skip whitespace, scan name (which may be parameter entity reference
                    // which is then expanded to a name)                    
                    ch = _current.SkipWhitespace();
                    if (ch == '%') 
                    {
                        Entity e = ParseParameterEntity(_ngterm);
                        PushEntity(_current.ResolvedUri, e);
                        ParseNameList(names, nmtokens);
                        PopEntity();
                        ch = _current.Lastchar;
                    }
                    else 
                    {
                        string token = _current.ScanToken(_sb, _ngterm, nmtokens);
                        string atom = _nt.Add(token.ToLower());
                        names.Add(atom);
                    }
                    ch = _current.SkipWhitespace();
                    if (ch == '|') ch = _current.ReadChar();
                }
                _current.ReadChar(); // consume ')'
            } 
            else 
            {
                string name = _current.ScanToken(_sb, _ws, nmtokens);
                name = _nt.Add(name.ToLower());
                names.Add(name);
            }
            return (string[])names.ToArray(typeof(String));
        }

        void ParseNameList(ArrayList names, bool nmtokens)
        {
            char ch = _current.Lastchar;
            ch = _current.SkipWhitespace();
            while (ch != Entity.EOF) 
            {
                string name;
                if (ch == '%') 
                {
                    Entity e = ParseParameterEntity(_ngterm);
                    PushEntity(_current.ResolvedUri, e);
                    ParseNameList(names, nmtokens);
                    PopEntity();
                    ch = _current.Lastchar;
                } 
                else 
                {
                    name = _current.ScanToken(_sb, _ngterm, true);
                    name = _nt.Add(name.ToLower());
                    names.Add(name);
                }
                ch = _current.SkipWhitespace();
                if (ch == '|') 
                {
                    ch = _current.ReadChar();
                    ch = _current.SkipWhitespace();
                }
            }
        }

        static string _dcterm = " \r\n\t>";
        ContentModel ParseContentModel(char ch)
        {
            ContentModel cm = new ContentModel();
            if (ch == '(') 
            {
                _current.ReadChar();
                ParseModel(')', cm);
                ch = _current.ReadChar();
                if (ch == '?' || ch == '+' || ch == '*') 
                {
                    cm.AddOccurrence(ch);
                    _current.ReadChar();
                }
            } 
            else if (ch == '%') 
            {
                Entity e = ParseParameterEntity(_dcterm);
                PushEntity(_current.ResolvedUri, e);
                cm = ParseContentModel(_current.Lastchar);
                PopEntity(); // bugbug should be at EOF.
            }
            else
            {
                string dc = ScanName(_dcterm);
                cm.SetDeclaredContent(dc);
            }
            return cm;
        }

        static string _cmterm = " \r\n\t,&|()?+*";
        void ParseModel(char cmt, ContentModel cm)
        {
            // Called when part of the model is made up of the contents of a parameter entity
            int depth = cm.CurrentDepth;
            char ch = _current.Lastchar;
            ch = _current.SkipWhitespace();
            while (ch != cmt || cm.CurrentDepth > depth) // the entity must terminate while inside the content model.
            {
                if (ch == Entity.EOF) 
                {
                    _current.Error("Content Model was not closed");
                }
                if (ch == '%') 
                {
                    Entity e = ParseParameterEntity(_cmterm);
                    PushEntity(_current.ResolvedUri, e);
                    ParseModel(Entity.EOF, cm);
                    PopEntity();                    
                    ch = _current.SkipWhitespace();
                } 
                else if (ch == '(') 
                {
                    cm.PushGroup();
                    _current.ReadChar();// consume '('
                    ch = _current.SkipWhitespace();
                }
                else if (ch == ')') 
                {
                    ch = _current.ReadChar();// consume ')'
                    if (ch == '*' || ch == '+' || ch == '?') 
                    {
                        cm.AddOccurrence(ch);
                        ch = _current.ReadChar();
                    }
                    if (cm.PopGroup() < depth)
                    {
                        _current.Error("Parameter entity cannot close a paren outside it's own scope");
                    }
                    ch = _current.SkipWhitespace();
                }
                else if (ch == ',' || ch == '|' || ch == '&') 
                {
                    cm.AddConnector(ch);
                    _current.ReadChar(); // skip connector
                    ch = _current.SkipWhitespace();
                }
                else
                {
                    string token;
                    if (ch == '#') 
                    {
                        ch = _current.ReadChar();
                        token = "#" + _current.ScanToken(_sb, _cmterm, true); // since '#' is not a valid name character.
                    } 
                    else 
                    {
                        token = _current.ScanToken(_sb, _cmterm, true);
                    }
                    token = _nt.Add(token.ToLower());// atomize it.
                    ch = _current.Lastchar;
                    if (ch == '?' || ch == '+' || ch == '*') 
                    {
                        cm.PushGroup();
                        cm.AddSymbol(token);
                        cm.AddOccurrence(ch);
                        cm.PopGroup();
                        _current.ReadChar(); // skip connector
                        ch = _current.SkipWhitespace();
                    } 
                    else 
                    {
                        cm.AddSymbol(token);
                        ch = _current.SkipWhitespace();
                    }                   
                }
            }
        }

        void ParseAttList()
        {
            char ch = _current.SkipWhitespace();
            string[] names = ParseNameGroup(ch, true);          
            AttList attlist = new AttList();
            ParseAttList(attlist, '>');
            foreach (string name in names) 
            {
                ElementDecl e = (ElementDecl)_elements[name];
                if (e == null) 
                {
                    _current.Error("ATTLIST references undefined ELEMENT {0}", name);
                }
                e.AddAttDefs(attlist);
            }
        }

        static string _peterm = " \t\r\n>";
        void ParseAttList(AttList list, char term)
        {
            char ch = _current.SkipWhitespace();
            while (ch != term) 
            {
                if (ch == '%') 
                {
                    Entity e = ParseParameterEntity(_peterm);
                    PushEntity(_current.ResolvedUri, e);
                    ParseAttList(list, Entity.EOF);
                    PopEntity();                    
                    ch = _current.SkipWhitespace();
                } 
                else if (ch == '-') 
                {
                    ch = ParseDeclComments();
                }
                else
                {
                    AttDef a = ParseAttDef(ch);
                    list.Add(a);
                }
                ch = _current.SkipWhitespace();
            }
        }

        AttDef ParseAttDef(char ch)
        {
            ch = _current.SkipWhitespace();
            string name = _nt.Add(ScanName(_ws).ToLower());
            AttDef attdef = new AttDef(name);

            ch = _current.SkipWhitespace();
            if (ch == '-') 
                ch = ParseDeclComments();               

            ParseAttType(ch, attdef);

            ch = _current.SkipWhitespace();
            if (ch == '-') 
                ch = ParseDeclComments();               

            ParseAttDefault(ch, attdef);

            ch = _current.SkipWhitespace();
            if (ch == '-') 
                ch = ParseDeclComments();               

            return attdef;

        }

        void ParseAttType(char ch, AttDef attdef)
        {
            if (ch == '%')
            {
                Entity e = ParseParameterEntity(_ws);
                PushEntity(_current.ResolvedUri, e);
                ParseAttType(_current.Lastchar, attdef);
                PopEntity(); // bugbug - are we at the end of the entity?
                ch = _current.Lastchar;
                return;
            }

            if (ch == '(') 
            {
                attdef.EnumValues = ParseNameGroup(ch, false);  
                attdef.Type = AttributeType.ENUMERATION;
            } 
            else 
            {
                string token = ScanName(_ws);
                if (token == "NOTATION") 
                {
                    ch = _current.SkipWhitespace();
                    if (ch != '(') 
                    {
                        _current.Error("Expecting name group '(', but found '{0}'", ch);
                    }
                    attdef.Type = AttributeType.NOTATION;
                    attdef.EnumValues = ParseNameGroup(ch, true);
                } 
                else 
                {
                    attdef.SetType(token);
                }
            }
        }

        void ParseAttDefault(char ch, AttDef attdef)
        {
            if (ch == '%')
            {
                Entity e = ParseParameterEntity(_ws);
                PushEntity(_current.ResolvedUri, e);
                ParseAttDefault(_current.Lastchar, attdef);
                PopEntity(); // bugbug - are we at the end of the entity?
                ch = _current.Lastchar;
                return;
            }

            bool hasdef = true;
            if (ch == '#') 
            {
                _current.ReadChar();
                string token = _current.ScanToken(_sb, _ws, true);
                hasdef = attdef.SetPresence(token);
                ch = _current.SkipWhitespace();
            } 
            if (hasdef) 
            {
                if (ch == '\'' || ch == '"') 
                {
                    string lit = _current.ScanLiteral(_sb, ch);
                    attdef.Default = lit;
                    ch = _current.SkipWhitespace();
                }
                else
                {
                    string name = _current.ScanToken(_sb, _ws, false);
                    name = _nt.Add(name.ToLower());
                    attdef.Default = name; // bugbug - must be one of the enumerated names.
                    ch = _current.SkipWhitespace();
                }
            }
        }
    }   
}
