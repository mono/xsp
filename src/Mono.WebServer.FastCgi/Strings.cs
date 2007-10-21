namespace Mono.FastCgi {
	internal static class Strings {
		public static string Server_MaxConnsOutOfRange = "At least one connection must be permitted.";
		public static string Server_MaxReqsOutOfRange = "At least one request must be permitted.";
		public static string Server_AlreadyStarted = "The server is already started.";
		public static string Server_NotStarted = "The server has not been started.";
		public static string Server_ValueUnknown = "Unknown value, {0}, requested by client.";
		public static string Server_Accepting = "Accepting an incoming connection.";
		public static string Server_AcceptFailed = "Failed to accept connection. Reason: {0}";
		public static string Server_ResponderDoesNotImplement = "Responder must implement the FastCgi.IResponder interface.";
		public static string Server_ResponderLacksProperConstructor = "Responder must contain public constructor {0}(ResponderRequest)";
		public static string Server_ResponderNotSupported = "Responder role is not supported.";
		public static string Logger_Format = "[{0:u}] {1,-7} {2}";
		public static string Connection_BeginningRun = "Beginning to receive records on connection.";
		public static string Connection_EndingRun = "Finished receiving records on connection.";
		public static string Connection_RecordNotReceived = "Failed to receive record.";
		public static string Connection_RequestAlreadyExists = "Request with given ID already exists.";
		public static string Connection_RoleNotSupported = "{0} role not supported by server.";
		public static string Connection_RequestDoesNotExist = "Request {0} does not exist."; 
		public static string Connection_AbortRecordReceived = "FastCGI Abort Request";
		public static string Connection_UnknownRecordType = "Unknown type, {0}, encountered.";
		public static string Connection_Terminating = "Terminating connection.";
		public static string NameValuePair_ParameterRead = "Read parameter. ({0} = {1})";
		public static string NameValuePair_DuplicateParameter = "Duplicate name, {0}, encountered. Overwriting existing value.";
		public static string NameValuePair_DictionaryContainsNonString = "Dictionary must only contain string values.";
		public static string NameValuePair_LengthLessThanZero = "Length must be greater than or equal to zero.";
		public static string Record_Received = "Record received. (Type: {0}, ID: {1}, Length: {2})";
		public static string Record_Sent = "Record sent. (Type: {0}, ID: {1}, Length: {2})";
		public static string Record_DataTooBig = "Data exceeds 65535 bytes and cannot be stored.";
		public static string Record_ToString = "FastCGI Record:\n   Version:        {0}\n   Type:           {0}\n   Request ID:     {0}\n   Content Length: {0}";
		public static string BeginRequestBody_WrongType = "The record's type is not BeginRequest.";
		public static string BeginRequestBody_WrongSize = "8 bytes expected.";
		public static string UnmanagedSocket_NotSupported = "Unmanaged sockets not supported.";
		public static string UnixSocket_AlreadyExists = "There's already a server listening on {0}";
		public static string ResponderRequest_IncompleteInput = "Insufficient input data received. (Expected {0} bytes but got {1}.)";
		public static string ResponderRequest_NoContentLength = "Content length parameter missing.";
		public static string ResponderRequest_NoContentLengthNotNumber = "Content length parameter not an integer.";
		public static string ResponderRequest_ContentExceedsLength = "Input data exceeds content length.";
		public static string Request_Aborting = "Aborting request {0}. Reason follows:";
		public static string Request_ParametersAlreadyCompleted = "The parameter stream has already been marked as closed. Ignoring record.";
		public static string Request_StandardInputAlreadyCompleted = "The standard input stream has already been marked as closed. Ignoring record.";
		public static string Request_FileDataAlreadyCompleted = "The file data stream has already been marked as closed. Ignoring record.";
		public static string Request_CanNotParseParameters = "Failed to parse parameter data.";
		public static string Request_NotStandardInput = "The record's type is not StandardInput.";
		public static string Request_NotFileData = "The record's type is not Data.";
	}
}