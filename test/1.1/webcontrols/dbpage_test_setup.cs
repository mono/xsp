using System;
using System.IO;
using Mono.Data.Sqlite;

class App
{
	static string[] emails = {
		"Joe Doe", "joe.doe@domain.com",
		"Jane Doe", "jane.doe@domain.com",
		"Bart Simpson", "bart@simpsons.com",
		"Donald Duck", "donald.duck@donaldinho.com",
		"Shrek Ogre", "shrek@farfaraway.com"
	};

	static object[] addresses = {
		1, "Joe Doe", "Somewhere, 12456",
		2, "Jane Doe", "Somewhere Else, 12345",
		3, "Bart Simpson", "Smallville, 12313",
		4, "Donald Duck", "Metropolis 13141",
		5, "Shrek Ogre", "The Swamp, 12314"
	};
	
	public static int Main ()
	{
		File.Delete ("dbpage1.sqlite");
		
		SqliteConnection conn = new SqliteConnection ();
		conn.ConnectionString = "URI=file:dbpage1.sqlite, Version=3";
		conn.Open ();
		
		SqliteCommand cmd = new SqliteCommand ();
		cmd.Connection = conn;
		cmd.CommandText =
			@"CREATE TABLE test (
			 person VARCHAR (256) NOT NULL,
			 email VARCHAR (256) NOT NULL
			)";
		cmd.ExecuteNonQuery ();

		cmd.CommandText =
			@"INSERT INTO test (person, email)
                         VALUES (:person, :email)";

		for (int i = emails.Length - 1; i > 0; i -= 2) {
			cmd.Parameters.Add (new SqliteParameter ("email", emails [i]));
			cmd.Parameters.Add (new SqliteParameter ("person", emails [i - 1]));
			cmd.ExecuteNonQuery ();
			cmd.Parameters.Clear ();
		}
		
		conn.Close ();

		File.Delete ("dbpage2.sqlite");
		conn = new SqliteConnection ();
		conn.ConnectionString = "URI=file:dbpage2.sqlite, Version=3";
		conn.Open ();

		cmd = new SqliteCommand ();
		cmd.Connection = conn;
		cmd.CommandText =
			@"CREATE TABLE customers (
                          id INTEGER NOT NULL,
                          name VARCHAR (256) NOT NULL,
                          address VARCHAR (256) NOT NULL
                         )";
		cmd.ExecuteNonQuery ();

		cmd.CommandText =
			@"INSERT INTO customers (id, name, address)
                         VALUES (:id, :name, :address)";

		for (int i = addresses.Length - 1; i > 0; i -= 3) {
			cmd.Parameters.Add (new SqliteParameter ("id", addresses [i - 2]));
			cmd.Parameters.Add (new SqliteParameter ("name", addresses [i - 1]));
			cmd.Parameters.Add (new SqliteParameter ("address", addresses [i]));
			
			cmd.ExecuteNonQuery ();
			cmd.Parameters.Clear ();
		}
		
		conn.Close ();
		
		return 0;
	}
}
