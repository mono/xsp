//
// Mono.ASPNET.Tools.DbSession
//
// Author(s):
//  Jackson Harper (jackson@ximian.com)
//
// (C) 2003 Novell, Inc (http://www.novell.com)
//

using System;
using System.Data;
using System.Reflection;
using System.Configuration;
using System.Collections.Specialized;

namespace Mono.ASPNET.Tools {

	internal sealed class DbSession {

		static string paramPrefix;

		private delegate void DbSessionCommand (IDbConnection conn);
		
		private static int Main (string [] args)
		{
			IDbConnection conn = GetConnection ();
			DbSessionCommand command = GetCommand (args);

                        try {
                                conn.Open ();
                                command (conn);
                        } catch {
                                throw;
                        } finally {
                                if (conn != null)
                                        conn.Close ();
                        }

                        return 0;
		}

                private static void Clean (IDbConnection conn)
                {
                        using (IDbCommand command = conn.CreateCommand ()) {
                                IDataParameterCollection param;
                                
                                command.CommandText = "DELETE FROM ASPStateTempSessions WHERE Expires < " + paramPrefix + "Now";
                                
                                param = command.Parameters;
                                param.Add (CreateParam (command, DbType.DateTime, "Now", DateTime.Now));
                                
                                command.ExecuteNonQuery ();
                        }
                }

                private static void Delete (IDbConnection conn)
                {
                        using (IDbCommand command = conn.CreateCommand ()) {
                                command.CommandText = "DELETE FROM ASPStateTempSessions";                                
                                command.ExecuteNonQuery ();
                        }
                }

                private static void Show (IDbConnection conn)
                {
                        Console.Write ("ID                                      ");
                        Console.Write ("Created                 ");
                        Console.Write ("Expires                 ");
                        Console.Write ("Timeout ");
                        Console.Write ("Data Size       ");
                        Console.Write ("Static Objects Size");
                        Console.WriteLine ();
                        
                        using (IDbCommand command = conn.CreateCommand ()) {
                                command.CommandText = "SELECT * FROM ASPStateTempSessions";

                                using (IDataReader reader = command.ExecuteReader ()) {
                                        while (reader.Read ()) {
                                                Console.Write (reader.GetString (0) + "\t");
                                                Console.Write (reader.GetDateTime (1) + "\t");
                                                Console.Write (reader.GetDateTime (2) + "\t");
                                                Console.Write (reader.GetInt32 (3) + "\t");
                                                Console.Write (reader.GetBytes (4, -1, null, -1, -1) + "\t\t");
                                                Console.Write (reader.GetBytes (5, -1, null, -1, -1) + "\t");
                                                Console.WriteLine ();
                                        }
                                }
			}
                }
                
                private static void Usage ()
                {
                        Console.WriteLine ("usage: dbsessmgr <command>");
                        Console.WriteLine ("Commands:");
                        Console.WriteLine ("If no command is specified --clean will be used.");
                        Console.WriteLine ("--clean     Remove all expired sessions");
                        Console.WriteLine ("--delete    Delete all sessions");
                        Console.WriteLine ("--show      Display session data");
                        Environment.Exit (1);
                }

		private static IDbConnection GetConnection ()
		{
			string asm, type, conn_str;
			IDbConnection conn;
			
			GetConnectionData (out asm, out type, out conn_str, out paramPrefix);
			
			Assembly dbAssembly = Assembly.LoadWithPartialName (asm);
			Type cnc_type = dbAssembly.GetType (type, true);
			if (!typeof (IDbConnection).IsAssignableFrom (cnc_type))
				throw new ApplicationException ("The type '" + cnc_type +
						"' does not implement IDB Connection.\n" +
						"Check 'DbConnectionType' in dbsessmgr.exe.config.");

			conn = (IDbConnection) Activator.CreateInstance (cnc_type);
			conn.ConnectionString = conn_str;

			return conn;
		}
		
		private static void GetConnectionData (out string asm,
				out string type, out string conn_str,
				out string param_prefix)
		{
			asm = null;
			type = null;
			conn_str = null;
			param_prefix = null;

			NameValueCollection config = ConfigurationManager.AppSettings;
			if (config != null) {
				asm = config ["DBProviderAssembly"];
				type = config ["DBConnectionType"];
				conn_str = config ["DBConnectionString"];
				param_prefix = config ["DBParamPrefix"];
			}

			if (asm == null || asm == String.Empty)
				asm = "Npgsql.dll";

			if (type == null || type == String.Empty)
				type = "Npgsql.NpgsqlConnection";

			if (conn_str == null || conn_str == String.Empty)
				conn_str = "SERVER=127.0.0.1;USER ID=monostate;PASSWORD=monostate;dbname=monostate";

			if (param_prefix == null || param_prefix == String.Empty)
				param_prefix = ":";
		}

		private static DbSessionCommand GetCommand (string [] args)
		{
                        DbSessionCommand cmd = null;
                        
			if (args.Length != 1)
                                return new DbSessionCommand (Clean);

                        switch (args [0]) {
                        case "--delete":
                                cmd = new DbSessionCommand (Delete);
                                break;
                        case "--show":
                                cmd = new DbSessionCommand (Show);
                                break;
                        
                        case "--clean":
                                cmd = new DbSessionCommand (Clean);
                                break;                                
                        case "--help":
                        default:
                                Usage ();
                                break;
                        }
                        return cmd;
		}

                private static IDataParameter CreateParam (IDbCommand command, DbType type,
				string name, object value)
		{
			IDataParameter result = command.CreateParameter ();
			result.DbType = type;
			result.ParameterName = paramPrefix + name;
			result.Value = value;
			return result;
		}
	}
}

