/*
* 
* An XmlReader implementation for loading HTML as if it was XHTML.
*
* Copyright (c) 2002 Microsoft Corporation. All rights reserved.
*
* Chris Lovett
* 
*/

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Text;
using System.Reflection;

namespace Sgml {
    class Attribute {
        public string Name;
        public AttDef DtdType;
        public char QuoteChar;

        public Attribute(string name, string value, char quote) {
            Name = name;
            _value = value;
            QuoteChar = quote;
        }

        public void Reset(string name, string value, char quote) {
            Name = name;
            _value = value;
            QuoteChar = quote;
            DtdType = null;
        }

        public string Value {
            get {
                if (_value != null) return _value;
                if (DtdType != null) return DtdType.Default;
                return null;
            }
            set {
                _value = value;
            }
        }

        public bool IsDefault {
            get {
                return (_value == null);
            }
        }
        string _value;
    }

    class Node {
        public XmlNodeType NodeType;
        public string Value;
        public XmlSpace Space;
        public string XmlLang;
        public bool IsEmpty;        
        public string Name;
        public ElementDecl DtdType;
        public State CurrentState;

        Attribute[] _attributes;
        int _attsize;
        int _attcount;

        public Node(string name, XmlNodeType nt, string value) {
            Name = name;
            NodeType = nt;
            Value = value;
            IsEmpty = true;
        }

        public void Reset(string name, XmlNodeType nt, string value) {           
            Value = value;
            Name = name;
            NodeType = nt;
            Space = XmlSpace.None;
            XmlLang= null;
            IsEmpty = true;
            _attcount = 0;
            DtdType = null;
        }
        public Attribute AddAttribute(string name, string value, char quotechar) {
            if (_attcount == _attsize) {
                int newsize = _attsize+10;
                Attribute[] newarray = new Attribute[newsize];
                if (_attributes != null)
                    Array.Copy(_attributes, newarray, _attsize);
                _attsize = newsize;
                _attributes = newarray;
            }
            for (int i = 0; i < _attcount; i++) {
                if ((object)_attributes[i].Name == (object)name) {
                    return null; // ignore duplicates!
                }
            }
            Attribute a = _attributes[_attcount];
            if (a == null) {
                a = new Attribute(name, value, quotechar);
                _attributes[_attcount] = a;
            } 
            else {
                a.Reset(name, value, quotechar);
            }
            _attcount++;
            return a;
        }

        public void RemoveAttribute(string name) {
            for (int i = 0; i < _attcount; i++) {
                if (_attributes[i].Name == name) {
                    _attributes[i] = null;
                    Array.Copy(_attributes, i+1, _attributes, i, _attcount - i - 1);
                    _attcount--;
                    return;
                }
            }
        }
        public void CopyAttributes(Node n) {
            for (int i = 0; i < n._attcount; i++) {
                Attribute a = n._attributes[i];
                Attribute na = this.AddAttribute(a.Name, a.Value, a.QuoteChar);
                na.DtdType = a.DtdType;
            }
        }

        public int AttributeCount {
            get {
                return _attcount;
            }
        }

        public int GetAttribute(string name) {
            if (_attcount>0) {
                for (int i = 0; i < _attcount; i++) {
                    Attribute a = _attributes[i];
                    if (a.Name == name) {
                        return i;
                    }
                }
            }
            return -1;
        }

        public Attribute GetAttribute(int i) {
            if (i>=0 && i<_attcount) {
                Attribute a = _attributes[i];
                return a;
            }
            return null;
        }
    }

    enum State {
        Initial,
        Markup,
        EndTag,
        Attr,
        AttrValue,
        Text,
        PartialTag,
        AutoClose,
        CData,
        PartialText,
        Eof
    }


    /// <summary>
    /// SgmlReader is an XmlReader API over any SGML document (including built in support for HTML).  
    /// </summary>
    public class SgmlReader : XmlReader {
        SgmlDtd _dtd;
        Entity _current;
        State _state;
        XmlNameTable _nametable;
        char _partial;
        object _endTag;

        Node[] _stack;
        int _depth;
        int _size;

        Node _node; // current node (except for attributes)
        // Attributes are handled separately using these members.
        Attribute _a;
        int _apos; // which attribute are we positioned on in the collection.

        Uri _baseUri;

        StringBuilder _sb;
        StringBuilder _name;
        TextWriter _log;

        // autoclose support
        Node _newnode;
        int _poptodepth;
        int _rootCount;

        string _href;
        string _ErrorLogFile;
        Entity _lastError;
        string _proxy;
        TextReader _inputStream;
        string _syslit;
        string _pubid;
        string _subset;
        string _docType;
        WhitespaceHandling _whitespaceHandling;

        public SgmlReader() {
            Init();    
            _nametable = new NameTable();
        }

        /// <summary>
        /// Specify the SgmlDtd object directly.  This allows you to cache the Dtd and share
        /// it across multipl SgmlReaders.  To load a DTD from a URL use the SystemLiteral property.
        /// </summary>
        public SgmlDtd Dtd {
            get { 
                LazyLoadDtd(this._baseUri);
                return _dtd; 
            }
            set { _dtd = value; }
        }

        private void LazyLoadDtd(Uri baseUri) {
            if (_dtd == null) {
                if (_syslit == null || _syslit == "") {
                    if (_docType != null && _docType.ToLower() == "html") {
                        Assembly a = typeof(SgmlReader).Assembly;
                        string name = a.FullName.Split(',')[0]+".Html.dtd";
                        Stream stm = a.GetManifestResourceStream(name);
                        StreamReader sr = new StreamReader(stm);
                        _dtd = SgmlDtd.Parse(baseUri, "HTML", null, sr, null, _proxy, _nametable);
                    }
                } else  {                  
                    if (_syslit.IndexOf("://")>0) {
                        baseUri = new Uri(_syslit);
                    } 
                    else {
                        // probably a local filename.
                        baseUri = new Uri("file://"+ _syslit.Replace("\\","/"));                    
                    }
                    _dtd = SgmlDtd.Parse(baseUri, _docType, _pubid, _syslit, _subset, _proxy, _nametable);
                }
            }
        }

        /// <summary>
        /// The name of root element specified in the DOCTYPE tag.
        /// </summary>
        public string DocType {
            get { return _docType; }
            set { _docType = value; }
        }

        /// <summary>
        /// The PUBLIC identifier in the DOCTYPE tag
        /// </summary>
        public string PublicIdentifier {
            get { return _pubid; }
            set { _pubid = value; }
        }

        /// <summary>
        /// The SYSTEM literal in the DOCTYPE tag identifying the location of the DTD.
        /// </summary>
        public string SystemLiteral {
            get { return _syslit; }
            set { _syslit = value; }
        }

        /// <summary>
        /// The DTD internal subset in the DOCTYPE tag
        /// </summary>
        public string InternalSubset {
            get { return _subset; }
            set { _subset = value; }
        }

        /// <summary>
        /// The input stream containing SGML data to parse.
        /// You must specify this property or the Href property before calling Read().
        /// </summary>
        public TextReader InputStream {
            get { return _inputStream; }
            set { _inputStream = value; Init();}
        }

        /// <summary>
        /// Sometimes you need to specify a proxy server in order to load data via HTTP
        /// from outside the firewall.  For example: "itgproxy:80".
        /// </summary>
        public string WebProxy {
            get { return _proxy; }
            set { _proxy = value; }
        }

        /// <summary>
        /// The base Uri is used to resolve relative Uri's like the SystemLiteral and
        /// Href properties.  This is a method because BaseURI is a read-only
        /// property on the base XmlReader class.
        /// </summary>
        public void SetBaseUri(string uri)  {
            _baseUri = new Uri(uri);
        }

        /// <summary>
        /// Specify the location of the input SGML document as a URL.
        /// </summary>
        public string Href {
            get { return _href; }
            set { _href = value; 
                Init();
                if (_baseUri == null) {
                    if (_href.IndexOf("://")>0) {
                        _baseUri = new Uri(_href);
                    } else {
                        _baseUri = new Uri("file:///"+Directory.GetCurrentDirectory()+"//");
                    }
                }
            }
        }

        /// <summary>
        /// DTD validation errors are written to this stream.
        /// </summary>
        public TextWriter ErrorLog {
            get { return _log; }
            set { _log = value; }
        }

        /// <summary>
        /// DTD validation errors are written to this log file.
        /// </summary>
        public string ErrorLogFile {
            get { return _ErrorLogFile; }
            set { _ErrorLogFile = value; 
                this.ErrorLog = new StreamWriter(value); }
        }

        void Log(string msg, params string[] args) {
            if (ErrorLog != null) {
                string err = String.Format(msg, args);
                if (_lastError != _current) {
                    err = err + "    " + _current.Context();
                    _lastError = _current;
                    ErrorLog.WriteLine("### Error:"+err);
                } else {
                    string path = "";
                    if (_current.ResolvedUri != null) {
                        path = _current.ResolvedUri.AbsolutePath;
                    }
                    ErrorLog.WriteLine("### Error in "+
                        path+"#"+
                        _current.Name+
                        ", line "+_current.Line + ", position " + _current.LinePosition + ": "+
                        err);
                }
            }
        }
        void Log(string msg, char ch) {
            Log(msg, ch.ToString());
        }


        void Init() {
            _state = State.Initial;
            _stack = new Node[10];
            _size = 10;       
            _depth = 0;
            _node = Push(null, XmlNodeType.Document, null);
            _node.IsEmpty = false;
            _sb = new StringBuilder();
            _name = new StringBuilder();
            _poptodepth = 0;
            _current = null;
            _partial = '\0';
            _endTag = null;
            _a = null;
            _apos = 0;
            _newnode = null;
            _poptodepth = 0;
            _rootCount = 0;
        }

        void Grow() {
            int inc = 10;
            int newsize = _size+inc;
            Node[] narray = new Node[newsize];
            Array.Copy(_stack, narray, _size);
            _size = newsize;
            _stack = narray;
        }
        Node Push(string name, XmlNodeType nt, string value) {
            if (_depth == _size) Grow();
            Node result;
            if (_stack[_depth] == null) {
                result = new Node(name, nt, value);
                _stack[_depth] = result;
            } 
            else {
                result = _stack[_depth];
                result.Reset(name, nt, value);
            }   
            _depth++;
            _node = result;
            return result;
        }

        Node Push(Node n) {
            // we have to do a deep clone of the Node object because
            // it is reused in the stack.
            Node n2 = Push(n.Name, n.NodeType, n.Value);
            n2.DtdType = n.DtdType;
            n2.IsEmpty = n.IsEmpty;
            n2.Space = n.Space;
            n2.XmlLang = n.XmlLang;
            n2.CurrentState = n.CurrentState;
            n2.CopyAttributes(n);
            _node = n2;
            return n2;
        }

        void Pop() {
            if (_depth > 1) {
                _depth--;
                _node = _stack[_depth-1];
            }
        }

        public override XmlNodeType NodeType {
            get { 
                if (_state == State.Attr) {
                    return XmlNodeType.Attribute;
                } 
                else if (_state == State.AttrValue) {
                    return XmlNodeType.Text;
                }
                else if (_state == State.EndTag || _state == State.AutoClose) {
                    return XmlNodeType.EndElement;
                }
                return _node.NodeType;
            }
        }

        public override string Name {
            get {
                return this.LocalName;
            }
        }

        public override string LocalName { 
            get {
                string result = null;
                if (_state == State.Attr) {
                    result = _a.Name;
                } 
                else if (_state == State.AttrValue) {
                    result = null;
                }
                else {
                    result = _node.Name;
                }

                return result;
            }
        }

        public override string NamespaceURI { 
            get {
                // SGML has no namespaces, unless this turned out to be an xmlns attribute.
                if (_state == State.Attr && _a.Name == "xmlns") {
                    return "http://www.w3.org/2000/xmlns/";
                }
                return String.Empty;
            }
        }

        public override string Prefix { 
            get {
                // SGML has no namespaces.
                return String.Empty;
            }
        }

        public override bool HasValue { 
            get {
                if (_state == State.Attr || _state == State.AttrValue) {
                    return true;
                }
                return (_node.Value != null);
            }
        }

        public override string Value { 
            get {
                if (_state == State.Attr || _state == State.AttrValue) {
                    return _a.Value;
                }
                return _node.Value;
            }
        }

        public override int Depth { 
            get {
                if (_state == State.Attr) {
                    return _depth;
                } 
                else if (_state == State.AttrValue) {
                    return _depth+1;
                }
                return _depth-1;
            }
        }

        public override string BaseURI { 
            get {
                return _baseUri == null ? "" : _baseUri.AbsoluteUri;
            }
        }

        public override bool IsEmptyElement { 
            get {
                if (_state == State.Markup || _state == State.Attr || _state == State.AttrValue) {
                    return _node.IsEmpty;
                }
                return false;
            }
        }
        public override bool IsDefault { 
            get {
                if (_state == State.Attr || _state == State.AttrValue) 
                    return _a.IsDefault;
                return false;
            }
        }
        public override char QuoteChar { 
            get {
                if (_a != null) return _a.QuoteChar;
                return '\0';
            }
        }

        public override XmlSpace XmlSpace { 
            get {
                for (int i = _depth-1; i > 1; i--) {
                    XmlSpace xs = _stack[i].Space;
                    if (xs != XmlSpace.None) return xs;
                }
                return XmlSpace.None;
            }
        }

        public override string XmlLang { 
            get {
                for (int i = _depth-1; i > 1; i--) {
                    string xmllang = _stack[i].XmlLang;
                    if (xmllang != null) return xmllang;
                }
                return String.Empty;
            }
        }

        public WhitespaceHandling WhitespaceHandling {
            get {
                return _whitespaceHandling;
            } 
            set {
                _whitespaceHandling = value;
            }
        }

        public override int AttributeCount { 
            get {
                if (_state == State.Attr || _state == State.AttrValue) 
                    return 0;
                if (_node.NodeType == XmlNodeType.Element ||
                    _node.NodeType == XmlNodeType.DocumentType)
                    return _node.AttributeCount;
                return 0;
            }
        }

        public override string GetAttribute(string name) {
            if (_state != State.Attr && _state != State.AttrValue) {
                int i = _node.GetAttribute(name);
                if (i>=0) return GetAttribute(i);
            }
            return null;
        }

        public override string GetAttribute(string name, string namespaceURI) {
            return GetAttribute(name); // SGML has no namespaces.
        }

        public override string GetAttribute(int i) {
            if (_state != State.Attr && _state != State.AttrValue) {
                Attribute a = _node.GetAttribute(i);
                if (a != null)
                    return a.Value;
            }
            throw new IndexOutOfRangeException();
        }

        public override string this [ int i ] { 
            get {
                return GetAttribute(i);
            }
        }

        public override string this [ string name ] { 
            get {
                return GetAttribute(name);
            }
        }

        public override string this [ string name,string namespaceURI ] { 
            get {
                return GetAttribute(name, namespaceURI);
            }
        }

        public override bool MoveToAttribute(string name) {
            int i = _node.GetAttribute(name);
            if (i>=0) {
                MoveToAttribute(i);
                return true;
            }
            return false;
        }

        public override bool MoveToAttribute(string name, string ns) {
            return MoveToAttribute(name);
        }

        public override void MoveToAttribute(int i) {
            Attribute a = _node.GetAttribute(i);
            if (a != null) {
                _apos = i;
                _a = a;
                _node.CurrentState = _state;//save current state.
                _state = State.Attr;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        public override bool MoveToFirstAttribute() {
            if (_node.AttributeCount>0) {
                MoveToAttribute(0);
                return true;
            }
            return false;
        }

        public override bool MoveToNextAttribute() {
            if (_state != State.Attr && _state != State.AttrValue) {
                return MoveToFirstAttribute();
            }
            if (_apos<_node.AttributeCount-1) {
                MoveToAttribute(_apos+1);
                return true;
            }
            return false;
        }

        public override bool MoveToElement() {
            if (_state == State.Attr || _state == State.AttrValue) {
                _state = _node.CurrentState;
                _a = null;
                return true;
            }
            return (_node.NodeType == XmlNodeType.Element);
        }

        bool IsHtml {
            get {
                return (_dtd != null && _dtd.Name != null && _dtd.Name.ToLower() == "html");
            }
        }

        public override bool Read() {
            if (_current == null) {
                LazyLoadDtd(this._baseUri);

                if (this.Href != null) {
                    _current = new Entity("#document", null, _href, this._proxy);
                } else if (this._inputStream != null) {
                    _current = new Entity("#document", null, this._inputStream, _proxy);           
                } else {
                    throw new InvalidOperationException("You must specify input either via Href or InputStream properties");
                }
                _current.Html = this.IsHtml;
                _current.Open(null, _baseUri);
                _baseUri = _current.ResolvedUri;
            }

            bool foundnode = false;
            while (! foundnode) {
                switch (_state) {
                    case State.Initial: 
                        _state = State.Markup;
                        _current.ReadChar();
                        goto case State.Markup;
                    case State.Eof:
                        if (_current.Parent != null) {
                            _current.Close();
                            _current = _current.Parent;
                        } 
                        else {
                            return false;
                        }
                        break;
                    case State.EndTag:
                        if (this._endTag == (object)_node.Name) {
                            Pop(); // we're done!
                            _state = State.Markup;
                            goto case State.Markup;                    
                        }                     
                        Pop(); // close one element
                        foundnode = true;// return another end element.
                        break;
                    case State.Markup:
                        if (_node.IsEmpty) {
                            Pop();
                        }
                        foundnode = ParseMarkup();
                        break;
                    case State.PartialTag:
                        Pop(); // remove text node.
                        _state = State.Markup;
                        foundnode = ParseTag(_partial);
                        break;
                    case State.AutoClose:
                        Pop(); // close next node.
                        if (_depth <= _poptodepth) {
                            _state = State.Markup;
                            Push(_newnode); // now we're ready to start the new node.
                            _state = State.Markup;
                        } 
                        foundnode = true;
                        break;
                    case State.CData:
                        foundnode = ParseCData();
                        break;
                    case State.Attr:
                        goto case State.AttrValue;
                    case State.AttrValue:
                        _state = State.Markup;
                        goto case State.Markup;
                    case State.Text:
                        Pop();
                        goto case State.Markup;
                    case State.PartialText:
                        if (ParseText(_current.Lastchar, false)) {
                            _node.NodeType = XmlNodeType.Whitespace;
                        }
                        foundnode = true;
                        break;
                }
                if (foundnode && _node.NodeType == XmlNodeType.Whitespace && _whitespaceHandling == WhitespaceHandling.None) {
                    // strip out whitespace (caller is probably pretty printing the XML).
                    foundnode = false;
                }
            }
            return true;
        }

        bool ParseMarkup() {
            char ch = _current.Lastchar;
            if (ch == '<') {
                ch = _current.ReadChar();
                return ParseTag(ch);
            } 
            else if (ch != Entity.EOF) {
                if (_node.DtdType != null && _node.DtdType.ContentModel.DeclaredContent == DeclaredContent.CDATA) {
                    // e.g. SCRIPT or STYLE tags which contain unparsed character data.
                    _partial = '\0';
                    _state = State.CData;
                    return false;
                }
                else if (ParseText(ch, true)) {
                    _node.NodeType = XmlNodeType.Whitespace;
                }
                return true;
            }
            _state = State.Eof;
            return false;
        }

        static string _declterm = " \t\r\n>";
        bool ParseTag(char ch) {
            if (ch == '!') {
                ch = _current.ReadChar();
                if (ch == '-') {
                    return ParseComment();
                } 
                else if (ch != '_' && !Char.IsLetter(ch)) {
                    // perhaps it's one of those nasty office document hacks like '<![if ! ie ]>'
                    string value = _current.ScanToEnd(_sb, "Recovering", ">"); // skip it
                    Log("Ignoring invalid markup '<!"+value+">");
                    return false;
                }
                else {
                    string name = _current.ScanToken(_sb, _declterm, false);
                    if (name == "DOCTYPE") {
                        ParseDocType();
                        // In SGML DOCTYPE SYSTEM attribute is optional, but in XML it is required,
                        // therefore if there is a PUBLIC identifier, but no SYSTEM literal then
                        // remove the PUBLIC identifier.
                        if (this.GetAttribute("SYSTEM") == null && this.GetAttribute("PUBLIC") != null) 
                        {
                          this._node.RemoveAttribute("PUBLIC");
                        }
                        _node.NodeType = XmlNodeType.DocumentType;
                        return true;
                    } 
                    else {
                        Log("Invalid declaration '<!{0}...'.  Expecting '<!DOCTYPE' only.", name);
                        _current.ScanToEnd(null, "Recovering", ">"); // skip it
                        return false;
                    }
                }
            } 
            else if (ch == '?') {
                _current.ReadChar();// consume the '?' character.
                ParsePI();
            }
            else if (ch == '/') {
                return ParseEndTag();
            }
            else {
                return ParseStartTag(ch);
            }
            return true;
        }

        static string _tagterm = " \t\r\n/>";
        static string _aterm = " \t\r\n=/>";
        static string _avterm = " \t\r\n>";
        bool ParseStartTag(char ch) {
            if (_tagterm.IndexOf(ch)>=0) {
                _sb.Length = 0;
                _sb.Append('<');
                _state = State.PartialText;
                return false;
            }
            string name = _current.ScanToken(_sb, _tagterm, false);
            name = _nametable.Add(name.ToLower());
            Node n = Push(name, XmlNodeType.Element, null);
            n.IsEmpty = false;
            Validate(n);
            ch = _current.SkipWhitespace();
            while (ch != Entity.EOF && ch != '>') {
                if (ch == '/') {
                    n.IsEmpty = true;
                    ch = _current.ReadChar();
                    if (ch != '>') {
                        Log("Expected empty start tag '/>' sequence instead of '{0}'", ch);
                        _current.ScanToEnd(null, "Recovering", ">");
                        return false;
                    }
                    break;
                } 
                else if (ch == '<') {
                    Log("Start tag '{0}' is missing '>'", name);
                    break;
                }
                string aname = _current.ScanToken(_sb, _aterm, false);
                ch = _current.SkipWhitespace();
                string value = null;
                char quote = '\0';
                if (ch == '=') {
                    _current.ReadChar();
                    ch = _current.SkipWhitespace();
                    if (ch == '\'' || ch == '\"') {
                        quote = ch;
                        value = ScanLiteral(_sb, ch);
                    } 
                    else if (ch != '>') {
                        string term = _avterm;
                        value = _current.ScanToken(_sb, term, false);
                    }
                } 
                aname = _nametable.Add(aname.ToLower());
                Attribute a = n.AddAttribute(aname, value, quote);
                if (a == null) {
                    Log("Duplicate attribute '{0}' ignored", aname);
                } else {
                    ValidateAttribute(n, a);
                }
                ch = _current.SkipWhitespace();
            }
            if (ch == Entity.EOF) {
                _current.Error("Unexpected EOF parsing start tag '{0}'", name);
            } 
            else if (ch == '>') {
                _current.ReadChar(); // consume '>'
            }
            if (this.Depth == 1) {
                if (_rootCount == 1) {
                    // Hmmm, we found another root level tag, soooo, the only
                    // thing we can do to keep this a valid XML document is stop
                    _state = State.Eof;
                    return false;
                }
                _rootCount++;
            }
            ValidateContent(n);
            return true;
        }

        bool ParseEndTag() {
            _state = State.EndTag;
            _current.ReadChar(); // consume '/' char.
            string name = _current.ScanToken(_sb, _tagterm, false);
            char ch = _current.SkipWhitespace();
            if (ch != '>') {
                Log("Expected empty start tag '/>' sequence instead of '{0}'", ch);
                _current.ScanToEnd(null, "Recovering", ">");
            }
            _current.ReadChar(); // consume '>'

            // Make sure there's a matching start tag for it.
            _endTag = _nametable.Add(name.ToLower());            
            _node = _stack[_depth-1];
            for (int i = _depth-1; i>0; i--) {
                if ((object)_stack[i].Name == _endTag)
                    return true;
            }
            Log("No matching start tag for '</{0}>'", name);
            _state = State.Markup;
            return false;
        }

        bool ParseComment() {
            char ch = _current.ReadChar();
            if (ch != '-') {
                Log("Expecting comment '<!--' but found {0}", ch);
                _current.ScanToEnd(null, "Comment", ">");
                return false;
            }
            string value = _current.ScanToEnd(_sb, "Comment", "-->");
            
            // Make sure it's a valid comment!
            int i = value.IndexOf("--");
            while (i>=0) {
                int j = i+2;
                while (j<value.Length && value[j]=='-')
                    j++;
                if (i>0) {
                    value = value.Substring(0, i-1)+"-"+value.Substring(j);
                } 
                else {
                    value = "-"+value.Substring(j);
                }
                i = value.IndexOf("--");
            }
            if (value.Length>0 && value[value.Length-1] == '-') {
                value += " "; // '-' cannot be last character
            }
            Push(null, XmlNodeType.Comment, value);         
            return true;
        }

        static string _dtterm = " \t\r\n>";
        void ParseDocType() {
            char ch = _current.SkipWhitespace();
            string name = _current.ScanToken(_sb, _dtterm, false);
            name = _nametable.Add(name.ToLower());
            Push(name, XmlNodeType.DocumentType, null);
            ch = _current.SkipWhitespace();
            if (ch != '>') {
                string subset = "";
                string pubid = "";
                string syslit = "";

                if (ch != '[') {
                    string token = _current.ScanToken(_sb, _dtterm, false);
                    token = _nametable.Add(token.ToUpper());
                    if (token == "PUBLIC") {
                        ch = _current.SkipWhitespace();
                        if (ch == '\"' || ch == '\'') {
                            pubid = _current.ScanLiteral(_sb, ch);
                            _node.AddAttribute(token, pubid, ch);                        
                        }
                    } 
                    else if (token != "SYSTEM") {
                        Log("Unexpected token in DOCTYPE '{0}'", token);
                        _current.ScanToEnd(null, "DOCTYPE", ">");
                    }
                    ch = _current.SkipWhitespace();
                    if (ch == '\"' || ch == '\'') {
                        token = _nametable.Add("SYSTEM");
                        syslit = _current.ScanLiteral(_sb, ch);
                        _node.AddAttribute(token, syslit, ch);                        
                    }
                    ch = _current.SkipWhitespace();
                }
                if (ch == '[') {
                    subset = _current.ScanToEnd(_sb, "Internal Subset", "]");
                    _node.Value = subset;
                }
                ch = _current.SkipWhitespace();
                if (ch != '>') {
                    Log("Expecting end of DOCTYPE tag, but found '{0}'", ch);
                    _current.ScanToEnd(null, "DOCTYPE", ">");
                }

                if (_dtd == null) {
                    this._docType = name;
                    this._pubid = pubid;
                    this._syslit = syslit;
                    this._subset = subset;
                    LazyLoadDtd(_current.ResolvedUri);
                }
            }           
            _current.ReadChar();
        }

        static string _piterm = " \t\r\n?";
        bool ParsePI() {
            string name = _current.ScanToken(_sb, _piterm, false);
            string value = null;
            if (_current.Lastchar != '?') {
                value = _current.ScanToEnd(_sb, "Processing Instruction", "?>");
            }
            else {
                // error recovery.
                value = _current.ScanToEnd(_sb, "Processing Instruction", ">");
            }
            Push(_nametable.Add(name), XmlNodeType.ProcessingInstruction, value);
            return true;
        }

        bool ParseText(char ch, bool newtext) {
            bool ws = !newtext || _current.IsWhitespace;
            if (newtext) _sb.Length = 0;
            //_sb.Append(ch);
            //ch = _current.ReadChar();
            _state = State.Text;
            while (ch != Entity.EOF) {
                if (ch == '<') {
                    ch = _current.ReadChar();
                    if (ch == '/' || ch == '!' || ch == '?' || Char.IsLetter(ch)) {
                        // Hit a tag, so return XmlNodeType.Text token
                        // and remember we partially started a new tag.
                        _state = State.PartialTag;
                        _partial = ch;
                        break;
                    } 
                    else {
                        // not a tag, so just proceed.
                        _sb.Append('<'); 
                        _sb.Append(ch);
                        ws = false;
                        ch = _current.ReadChar();
                    }
                } 
                else if (ch == '&') {
                    ExpandEntity(_sb, '<');
                    ws = false;
                    ch = _current.Lastchar;
                }
                else {
                    if (!_current.IsWhitespace) ws = false;
                    _sb.Append(ch);
                    ch = _current.ReadChar();
                }
            }
            string value = _sb.ToString();
            Push(null, XmlNodeType.Text, value);
            return ws;
        }

        // This version is slightly different from Entity.ScanLiteral in that
        // it also expands entities.
        public string ScanLiteral(StringBuilder sb, char quote) {
            sb.Length = 0;
            char ch = _current.ReadChar();
            while (ch != Entity.EOF && ch != quote ) {
                if (ch == '&') {
                    ExpandEntity(_sb, quote);
                    ch = _current.Lastchar;
                }               
                else {
                    sb.Append(ch);
                    ch = _current.ReadChar();
                }
            }
            _current.ReadChar(); // consume end quote.          
            return sb.ToString();
        }

        bool ParseCData() {
            // Like ParseText(), only it doesn't allow elements in the content.  
            // It allows comments and processing instructions and text only and
            // text is not returned as text but CDATA (since it may contain angle brackets).
            // And initial whitespace is ignored.  It terminates when we hit the
            // end tag for the current CDATA node (e.g. </style>).
            bool ws = _current.IsWhitespace;
            _sb.Length = 0;
            char ch;
            if (_partial != '\0') {
                Pop(); // pop the CDATA
                switch (_partial) {
                    case '!':
                        _partial = ' '; // and pop the comment next time around
                        return ParseComment();
                    case '?':
                        _partial = ' '; // and pop the PI next time around
                        return ParsePI();
                    case '/':
                        _state = State.EndTag;
                        return true;    // we are done!
                    case ' ':
                        break; // means we just needed to pop the CDATA.
                }
            }
            // if _partial == '!' then parse the comment and return
            // if _partial == '?' then parse the processing instruction and return.
            ch = _current.ReadChar();
            while (ch != Entity.EOF) {
                if (ch == '<') {
                    ch = _current.ReadChar();
                    if (ch == '!') {
                        ch = _current.ReadChar();
                        if (ch == '-') {
                            // return what CDATA we have accumulated so far
                            // then parse the comment and return to here.
                            if (ws) {
                                _partial = ' '; // pop comment next time through
                                return ParseComment();
                            } 
                            else {
                                // return what we've accumulated so far then come
                                // back in and parse the comment.
                                _partial = '!';
                                break; 
                            }
                        } 
                        else {
                            // not a comment, so ignore it and continue on.
                            _sb.Append('<');
                            _sb.Append('!');
                            _sb.Append(ch);
                            ws = false;
                        }
                    } 
                    else if (ch == '?') {
                        // processing instruction.
                        _current.ReadChar();// consume the '?' character.
                        if (ws) {
                            _partial = ' '; // pop PI next time through
                            return ParsePI();
                        } 
                        else {
                            _partial = '?';
                            break;
                        }
                    }
                    else if (ch == '/') {
                        // see if this is the end tag for this CDATA node.
                        string temp = _sb.ToString();
                        if (ParseEndTag() && _endTag == (object)_node.Name) {
                            if (ws) {
                                // we are done!
                                return true;
                            } 
                            else {
                                // return CDATA text then the end tag
                                _partial = '/';
                                _sb.Length = 0; // restore buffer!
                                _sb.Append(temp); 
                                _state = State.CData;
                                break;
                            }
                        } 
                        else {
                            // wrong end tag, so continue on.
                            _sb.Length = 0; // restore buffer!
                            _sb.Append(temp); 
                            _sb.Append("</"+_endTag+">");
                            ws = false;
                        }
                    }
                    else {
                        // must be just part of the CDATA block, so proceed.
                        _sb.Append('<'); 
                        _sb.Append(ch);
                        ws = false;
                    }
                } 
                else {
                    if (!_current.IsWhitespace && ws) ws = false;
                    _sb.Append(ch);
                }
                ch = _current.ReadChar();
            }
            string value = _sb.ToString();
            Push(null, XmlNodeType.CDATA, value);
            if (_partial == '\0')
                _partial = ' ';// force it to pop this CDATA next time in.
            return true;
        }

        void ExpandEntity(StringBuilder sb, char terminator) {
            char ch = _current.ReadChar();
            if (ch == '#') {
                string charent = _current.ExpandCharEntity();
                sb.Append(charent);
                ch = _current.ReadChar();
            } 
            else {
                _name.Length = 0;
                while (ch != Entity.EOF && 
                    (Char.IsLetter(ch) || ch == '_' || ch == '-')) {
                    _name.Append(ch);
                    ch = _current.ReadChar();
                }
                string name = _name.ToString();
                if (_dtd != null && name != "") {
                    Entity e = (Entity)_dtd.FindEntity(name);
                    if (e != null) {
                        if (e.Internal) {
                            sb.Append(e.Literal);
                            if (ch != terminator) 
                                ch = _current.ReadChar();
                            return;
                        } 
                        else {
                            Entity ex = new Entity(name, e.PublicId, e.Uri, _current.Proxy);
                            e.Open(_current, new Uri(e.Uri));
                            _current = ex;
                            _current.ReadChar();
                            return;
                        }
                    } 
                    else {
                        Log("Undefined entity '{0}'", name);
                    }
                }
                // Entity is not defined, so just keep it in with the rest of the
                // text.
                sb.Append("&");
                sb.Append(name);
                if (ch != terminator) {
                    sb.Append(ch);
                    ch = _current.ReadChar();
                }
            }
        }

        public override bool EOF { 
            get {
                return _state == State.Eof;
            }
        }

        public override void Close() {
            if (_current != null) {
                _current.Close();
                _current = null;
            }
            if (_log != null) {
                _log.Close();
                _log = null;
            }
        }

        public override ReadState ReadState { 
            get {
                if (_state == State.Initial) return ReadState.Initial;
                else if (_state == State.Eof) return ReadState.EndOfFile;
                return ReadState.Interactive;
            }
        }

        public override string ReadString() {
            if (_node.NodeType == XmlNodeType.Element) {
                _sb.Length = 0;
                while (Read()) {
                    switch (this.NodeType) {
                        case XmlNodeType.CDATA:
                        case XmlNodeType.SignificantWhitespace:
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.Text:
                            _sb.Append(_node.Value);
                            break;
                        default:
                            return _sb.ToString();
                    }
                }
                return _sb.ToString();
            }
            return _node.Value;
        }


      public override string ReadInnerXml()
      {
        StringWriter sw = new StringWriter();
        XmlTextWriter xw = new XmlTextWriter(sw);
        xw.Formatting = Formatting.Indented;
        switch (this.NodeType) 
        {
          case XmlNodeType.Element:
            Read();
            while (!this.EOF && this.NodeType != XmlNodeType.EndElement) 
            {
              xw.WriteNode(this, true);
            }
            Read(); // consume the end tag
            break;
          case XmlNodeType.Attribute:
            sw.Write(this.Value);
            break;
         default:
            // return empty string according to XmlReader spec.
            break;
        }
        xw.Close();
        return sw.ToString();
      }

      public override string ReadOuterXml()
      {
        StringWriter sw = new StringWriter();
        XmlTextWriter xw = new XmlTextWriter(sw);
        xw.Formatting = Formatting.Indented;
        xw.WriteNode(this, true);
        xw.Close();
        return sw.ToString();
      }

        public override XmlNameTable NameTable { 
            get {
                return _nametable;
            }
        }

        public override string LookupNamespace(string prefix) {           
            return null;// there are no namespaces in SGML.
        }

        public override void ResolveEntity() {
          // We never return any entity reference nodes, so this should never be called.
          throw new InvalidOperationException("Not on an entity reference.");
        }

        public override bool ReadAttributeValue() {
            if (_state == State.Attr) {
                _state = State.AttrValue;
                return true;
            } 
            else if (_state == State.AttrValue) {
                return false;
            }
            throw new InvalidOperationException("Not on an attribute.");
        }   

        void Validate(Node node) {
            if (_dtd != null) {
                ElementDecl e = _dtd.FindElement(node.Name);
                if (e != null) {
                    node.DtdType = e;
                    if (e.ContentModel.DeclaredContent == DeclaredContent.EMPTY) 
                        node.IsEmpty = true;
                }
            }
        }

        void ValidateAttribute(Node node, Attribute a) {
            ElementDecl e = node.DtdType;
            if (e != null) {
                AttDef ad = e.AttList[a.Name];
                if (ad != null) {
                    a.DtdType = ad;
                }
            }
        }   

        void ValidateContent(Node node) {
            if (_dtd != null) {
                // See if this element is allowed inside the current element.
                // If it isn't, then auto-close elements until we find one
                // that it's allowed to be in.  
                string name = node.Name; 
                int i = 0;
                int top = _depth-2;
                if (_dtd.FindElement(name) != null) { 
                    // it is a known element, let's see if it's allowed in the
                    // current context.
                    for (i = top; i>0; i--) {
                        Node n = _stack[i];
                        ElementDecl f = n.DtdType;
                        if (f != null) {
                            if (f.Name == _dtd.Name)
                              break; // can't pop the root element.
                            if (f.CanContain(name, _dtd)) 
                            {
                                break;
                            } 
                            else if (!f.EndTagOptional) {
                                // If the end tag is not optional then we can't
                                // auto-close it.  We'll just have to live with the
                                // junk we've found and move on.
                                break;
                            }
                        } 
                        else {
                            // Since we don't understand this tag anyway,
                            // we might as well allow this content!
                            break;
                        }
                    }
                }
                if (i == 0) {
                    // Tag was not found or is not allowed anywhere, ignore it and 
                    // continue on.
                }
                else if (i < top) {
                    if (i == top - 1 && name == _stack[top].Name) {
                        // e.g. p not allowed inside p, not an interesting error.
                    } else {
                        string closing = "";
                        for (int k = top; k >= i+1; k--) {
                            if (closing != "") closing += ",";
                            closing += "<"+_stack[k].Name+">";
                        }
                        Log("Element '{0}' not allowed inside '{1}', closing {2}.", 
                            name, _stack[top].Name, closing);
                    }
                    _state = State.AutoClose;
                    _newnode = node;
                    Pop(); // save this new node until we pop the others
                    _poptodepth = i+1;
                }
            }
        }
    }
}
