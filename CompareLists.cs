using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
/*
 * Compare Lists
 * Author: Casey Pyburn
 * Last Updated: May 13th, 2020
 */

public class compareLists{
	// for debugging
	
	public static void Main(String[] args){
		// process the arguments
		bool printLists = false,
				testContains = false,
				verbose = false,
				showMissing = false,
				getHelp = false,
				customMessages = false;
		// test for no arguments
		if(args.Length == 0){
			PrintHelp(verbose);
			return;
		}
		List<string[]> userLists = new List<string[]>(),
						fileLists = new List<string[]>();
		List<string> messages = new List<string>();
		for (int i = 0; i < args.Length; i++){
			string s = args[i];
			s.ToLower();
			if(s.Equals("/p"))
				printLists = true;
			else if(s.Equals("/c"))
				testContains = true;
			else if(s.Equals("/v"))
				verbose = true;
			else if(s.Equals("/?") || s.Equals("/h") || s.Equals("/help"))
				getHelp = true;
			else if(s.Equals("/n"))
				showMissing = true;
			else if(s.Equals("/m")){
				customMessages = true;
				messages.Add("The following users should be updated with high priority");
				messages.Add("The following users are missing the driver");
				messages.Add("The following users are missing the firmware");
				messages.Add("The following users were not listed in the log files");
				messages.Add("The following users were not listed as having run the driver");
				messages.Add("The following users were not listed as having run the firmware");
				// TO-Do: add functionality for using specialized messages for output
			}
			else if(s.Equals("/u")){
				// the /u switch indicates what files will be used for the user comparison
				userLists.Add(System.IO.File.ReadAllLines("Updates before 11TG19.txt"));
				userLists.Add(System.IO.File.ReadAllLines("completed users.txt"));
				// standardize the usernames
				for (int j = 0; j < userLists.Count; j++){
					// string[] ProcessUserList(string[] updateIn)
					userLists[j] = ProcessUserList(userLists[j]);
					Array.Sort(userLists[j]);
				}
			}
			else if(s.Equals("/f")){
				// the /f switch indicates what files will be used for the file comparison
				fileLists.Add(System.IO.File.ReadAllLines("driver.txt"));
				fileLists.Add(System.IO.File.ReadAllLines("firmware.txt"));
				// standardize the files
				for (int j = 0; j < fileLists.Count; j++){
					// string[] ProcessDriverList(string[] listIn)
					fileLists[j] = ProcessDriverList(fileLists[j]);
					Array.Sort(fileLists[j]);
				}
			}
		}
		if(getHelp)
			PrintHelp(verbose);
		else{
			// compare the user lists to the file lists
			Console.WriteLine("\n********************************************************************************\n");
			Console.WriteLine("Comparing users to file lists");
			Console.WriteLine("\n********************************************************************************\n");
			RunCompare(printLists, testContains, verbose, showMissing, userLists, fileLists, messages);
			// compare the files to eachother - hackish, and not abstracted, remove for full release
			Console.WriteLine("\n********************************************************************************\n");
			Console.WriteLine("File to File Comparisons");
			Console.WriteLine("\n********************************************************************************\n");
			List<string[]> file1 = new List<string[]>();
			file1.Add(fileLists[0]);
			List<string[]> file2 = new List<string[]>();
			file2.Add(fileLists[1]);
			RunCompare(printLists, testContains, verbose, showMissing, file1, file2, messages);
			RunCompare(printLists, testContains, verbose, showMissing, file2, file1, messages);
		}
		
	}
	
	private static void RunCompare(bool printLists, bool testContains, bool verbose, bool showMissing, List<string[]> userLists, List<string[]> fileLists, List<string> messages){
		// a surprise tool thta will help us later
		List<string[]> allLists = new List<string[]>();
		allLists.AddRange(userLists);
		allLists.AddRange(fileLists);
		// debuging tests
		if(printLists){
			foreach(string[] list in allLists){
				foreach(string s in list){
					PrintName(s);
				}
			}
		}
		if(testContains){
			foreach(string[] names in allLists){
				TestContains(names);
			}
		}
		// compare the lists and print the results
		// cycle through the user lists
		List<bool[]> results = new List<bool[]>();
		bool[] result;
		string[] users;
		List<List<string>> strRes;
		for(int j = 0; j < userLists.Count; j++){
			if(verbose)
				Console.WriteLine("Comparing users from list " +(j+1));
			users = userLists[j];
			// cycle through the file lists
			result = new bool[users.Length];
			if(verbose)
				Console.WriteLine("Result list length: "+ users.Length);
			foreach(string[] files in fileLists){
				if(verbose)
					Console.WriteLine("\n*** Start File Search ***\n");
				// cycle through the users and find who is in the current file list
				for(int i = 0; i < users.Length; i++){
					// fill results
					result[i] = Contains(files, users[i]);
					if(verbose){
						Console.WriteLine("User found in the file: "+result[i]+" - "+users[i]);
					}
				}
				// add the results
				results.Add(result);
				/* this works because each run of the loop allocates new memory to result
				 * the values from the prior run of the loop are maintained in the List */
				if(verbose)
					Console.WriteLine("\n*** End File Search ***\n");
			}
			/* At this point we have lists of results for this user list. Now to process them.
			 * How do you show common or uncommon values with just a single boolean switch?
			 * At first glance it would appear that the most efficient method would be to have
			 * an if/else that way you're not evaluating showMissing for every result.  It's more
			 * lines, but it will be more efficient in the end.  Or, you could further abstract it,
			 * and make a method that returns only the results that are correct.  Fewer lines, and
			 * roughly the same number of comparisons since you'd have to evaluate each value of the
			 * results against a known value anyway.
			 */
			 
			 //ProcessResults (List<string> users, List<bool[]> resultSubset, bool compOp, int subsetCount)
			 // don't forget to invert showMissing, since when it is true, compOp in ProcessResults should be false
			 strRes = ProcessResults(users, results, !showMissing, fileLists.Count);
			 // print out the results
			 Console.WriteLine("The results for user list "+(j+1)+" are as follows:");
			 PrintResults(strRes, messages, !showMissing);
		}
	}
	private static void PrintResults(List<List<string>> results, List<string> messages, bool compOp){
		// not going to work in the custom messages for now- too much work and I need to finish this...
		List<string> values;
		// print out the common/uncommon values (results[0]), so the loop doesn't constantly test for i == 0
		values = results[0];
		Console.WriteLine("The following are the "+(compOp == true ? "common" : "uncommon")+" values for all file lists:");
		foreach(string s in values){
			Console.WriteLine("\t"+s);
		}
		Console.WriteLine("Count: "+ values.Count);
		// printing results for the file lists
		for(int i = 1; i < results.Count; i++){
			// just so I don't get confused and confounded
			values = results[i];
			Console.WriteLine("The following are the unique values that "+(compOp == true ? "were" : "were not")+" in file list " +i);
			foreach(string s in values){
				Console.WriteLine("\t"+s);
			}
			Console.WriteLine("Count: "+ values.Count);
		}
	}
	private static List<List<string>> ProcessResults (string[] users, List<bool[]> resultSubset, bool compOp, int subsetCount){
		// compOp = true --> looking for common values, false, uncommon values
		List<List<string>> results = new List<List<string>>();
		// fill results with as many lists as there are elements of resultSubset, +1 for the culmination of them
		for(int i = 0; i < subsetCount +1; i++){
			results.Add(new List<string>());
		}
		// for simplicity, the 0th element will always be the results of the compiled subsets, and the following will be
		// the results unique to each subset
		
		// we want to return the values are (not) in all the result subsets, and the ones that are unique to each result subset
		bool aux = false;
		for (int i = 0; i < users.Length; i++){
			// look for the users that are (not) present in all subsets
			foreach(bool[] boolRes in resultSubset){
				// do they all match the operation?
				aux = compOp == boolRes[i];
			}
			// aux will only be true IFF the value at user i was the same as the comparison operation (see comment below the method header)
			if(aux){
				// add this user to the set of those which are (not) present in each subset
				results[0].Add(users[i]);
			}
			// if the above fails, this user could be unique to a subset
			else{
				/* NOTE: the possiblity exists for the compOp to succede in a subset of the subsets, but not all subsets.
				 * For now the first instance of a value that isn't common to all subsets will be considered unique.
				 * At a later date i might come back to this and find some way to better handle such cases.
				 */
				for(int j = 0; j < resultSubset.Count; j++){
					if(compOp == resultSubset[j][i]){
						// remember j+1 is the index in the list of values unique to resultSubset[j]
						results[j+1].Add(users[i]);
						// exit this loop
						break;
					}
				}
			}
		}
		
		return results;
	}
	private static bool Contains(string[] list, string s){
		// runs a binary search to find string s in the list
		int start = 0, end = list.Length - 1, mid;
		string currentStr;
		while(start <= end){
			mid = (start + end)/2;
			currentStr = list[mid];
			if(currentStr.CompareTo(s) < 0){
				start = mid + 1;
			}
			else if(currentStr.CompareTo(s) > 0){
				end = mid - 1;
			}
			else
				return true;
		}
		return false;
	}
	private static void TestContains(string[] list){
		Console.WriteLine("Testing list contains against itself");
			 bool[] results = new bool[list.Length];
			 int trueCount = 0;
			 for (int i = 0; i < list.Length; i++){
				 results[i] = Contains(list, list[i]);
				 if(results[i] == true)
					 trueCount++;
			 }
			 bool successful = true;
			 foreach(bool result in results){
				 if(result == false){
					 Console.WriteLine("Contains failed");
					 // print out the results
					 for(int i = 0; i < results.Length; i++){
						 Console.WriteLine(i+": "+results[i]);
					 }
					 successful = false;
					 break;
				 }
			 }
			 if(successful){
				 Console.WriteLine("Contains testing completed successfully.");
			 }
			 else{
				 Console.WriteLine("Contains failed");
			 }
			 Console.WriteLine("Count of true entries was: "+trueCount+" / "+list.Length);
	}
	private static void PrintName(string s){
		Regex whitespace = new Regex("\\s+");
		if(s == null || s.Equals("") || whitespace.IsMatch(s)){
			Console.WriteLine("-- Empty --");
		}
		else
			Console.WriteLine(s);
	}
	private static void PrintList(string[] list){
		foreach(string s in list){
				 PrintName(s);
			 }
			 Console.WriteLine("\n");
	}
	private static string[] ProcessUserList(string[] updateIn){
		// make a new array out of the updatees that is their username, sorted alphabeticly
		 string[] usernames = new string[updateIn.Length], 
				aux = new string[3];
		Regex fnameLname = new Regex("^([a-z]+)\\s([a-z]+)$"),
				fnameMLname = new Regex("^([a-z]+)\\s([a-z]+)\\s([a-z]+)$"),
				fnameMDotLname = new Regex("^([a-z]+)\\s([a-z]+)\\.\\s([a-z]+)$"),
				fnameLAPSname = new Regex("^([a-z]+)\\s([a-z]\\\'[a-z]+)$");
		 for(int i = 0; i < updateIn.Length; i++){
			 // send it to lower case
			 usernames[i] = updateIn[i].ToLower();
			 // need to test to see what format updates[i] is in
			 // test for [fname] [lname]
			 if(fnameLname.IsMatch(usernames[i])){
				 aux = usernames[i].Split(' ');
				 usernames[i] = aux[0].Substring(0,1) + aux[1];
			 }
			 // [fname] [middle] [lname]
			 else if(fnameMLname.IsMatch(usernames[i])){
				 aux = usernames[i].Split(' ');
				 usernames[i] = aux[0].Substring(0,1) + aux[1].Substring(0,1) + aux[2];
			 }
			 // [fname] [middle]. [lname]
			 else if(fnameMDotLname.IsMatch(usernames[i])){
				 aux = usernames[i].Split(' ');
				 // pull the . off of the end of aux[1]
				 int delIndex = aux[1].IndexOf('.');
				 aux[1] = aux[1].Remove(delIndex);
				 usernames[i] = aux[0].Substring(0,1) + aux[1] + aux[2];
			 }
			 // [fname] [lInitial]'[lname]
			 else if(fnameLAPSname.IsMatch(usernames[i])){
				 aux = usernames[i].Split(' ');
				 // pull the ' out
				 int delIndex = aux[1].IndexOf('\'');
				 aux[1] = aux[1].Remove(delIndex, delIndex);
				 usernames[i] = aux[0].Substring(0,1) + aux[1];
			 }
			 // if it failed the above tests then it should be Flname
			 // no further action required
		}
		return usernames;
	}
	private static string[] ProcessDriverList(string[] listIn){
		string[] result = new string[listIn.Length];
		int endIndex = 0;
		// fill result
		for(int i = 0; i < result.Length; i++){
			// find the first index of a -
			endIndex = listIn[i].IndexOf('-', 0);
			// add the string which is all characters before the - to the result
			result[i] = listIn[i].Substring(0, endIndex).ToLower();
		}
		return result;
	}
	
	private static void PrintHelp(bool verbose){
		// TO-DO: make this method print out a help message
		Console.WriteLine("CompareLists:");
		Console.WriteLine("This program compares a list of users to a list of log files. For best results\n"+
							"use a list of users in [first name] [lastname] format, and the output of \"dir /b\".\n"+
							"It is intended for command line use only. Redirection can be used to pipe the output\n"+
							"to a file. The default action of this program is to show the common values of the \n"+
							"user lists and the file lists.");
		
		if(verbose){
			Console.WriteLine("\n--- Program behavior ---");
			Console.WriteLine("The program takes a list of text files as arguments at the command line and uses\n"+
								"those to populate string arrays. There are two types of lists the program is\n"+
								"designed to handel: a list of usernames, and a list of log files. Both types are\n"+
								"assumed to have specific formats, and are standardized to follow the convention\n"+
								"of [first initial][middle name or initial if exists][last name].");
			Console.WriteLine("User Lists standardization:");
			Console.WriteLine("The following regular expressions are used to determine the format of the username:\n"+
								"\"^([a-z]+)\\s([a-z]+)$\", \"^([a-z]+)\\s([a-z]+)\\s([a-z]+)$\",\n"+
								"\"^([a-z]+)\\s([a-z]+)\\.\\s([a-z]+)$\", \"^([a-z]+)\\s([a-z]\\\'[a-z]+)$\"\n"+
								"Depending on which format the username matches the program removes unwanted\n"+
								"characters and creates a new list containing the standardized values. If the\n"+
								"original value is in the desired format, is in a format other than those listed\n"+
								"above, that value is added to the standardized list.");
			Console.WriteLine("File Lists standardization:");
			Console.WriteLine("For the file lists the following is assumed for the format of the text.\n"+
								"The file contains text in the format of [computername]-[some text we don't care about]\n"+
								"It splits that text using the \'-\' as a delimiter and taks the front half.\n"+
								"At some point that ability to change the delimiter may be added.");
			Console.WriteLine("Comparing the lists:");
			Console.WriteLine("After the input values have been standardized they are sorted using .Net's\n"+
								"naitive Array.Sort() method. The program then uses a binary search of the file\n"+
								"lists to determine if the each user exists in each file list.");
			Console.WriteLine("Order of comparisons:");
			Console.WriteLine("The user list(s) is/are tested against the file list(s). If there is more than\n"+
								"one user list the first file on the command line is tested first, then the\n"+
								"second, and so on. Currently the determination of if a user is unique to a file\n"+
								"list is done by finding the first instance of that user in the file lists.  Due\n"+
								"to this it is not reccomended to use more than 2 file lists.");
			Console.WriteLine("\n--- Debugging options ---");
			Console.WriteLine("\t/p\tPrnts out the contents of the lists after standardizing and sorting.");
			Console.WriteLine("\t/c\tTests the private method Contains() against each list using itself.\n"+
								"\t\tPrints the result and the count of items for each list.");
		}
		else{
			Console.WriteLine("For a detailed description of how this program works, and debugging options, use /? /v for switches");
		}
		
		Console.WriteLine("\n--- Options ---");
		Console.WriteLine("\t/u\tIndicates whate file(s) to use as the list(s) of user(s)");
		Console.WriteLine("\t/f\tIndicates whate file(s) to use as the list(s) of files to which to\n"+
							"\t\tcompare the user list(s)");
		Console.WriteLine("\t/n\tChanges what the program will show for resuts. \n"+
							"\t\tInstead of showing matches it shows which users are missing\n"+
							"\t\tfrom the file list(s)");
		Console.WriteLine("\t/v\tThis will cause the program to print results of the compasison as it goes\n"+
							"\t\t(verbose output). Not reccomended unless you're seeing something unexpected.");
		Console.WriteLine("\t/s\tOnly available when used with /v. Suppresses printing the summary\n"+
							"\t\tafter running the comparisons.");
		Console.WriteLine("\nThe switches can be in any order, with the exception that locaitons of text files\n"+
							"MUST follow the /u and /f switches. The switches are not case sensitive.");
		Console.WriteLine("Example: compareLists.exe /u usernames.txt /f files.txt");
	}
}