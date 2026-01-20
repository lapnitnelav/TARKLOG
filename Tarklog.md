## GOAL :
Build a .Net Application targeting Windows Desktop users to achieve the following goals :

  * A graphical user interface with multiple panes/windows (summary, history/stats, settings)
  * Feature 1 will be to scan a given directory (LOGROOT) for subfolders, move all of them except the most recent one to a different directory (LOGSTORAGE), ensure a safe transition of folders and no duplicates.
  * Feature 2 will be to find and parse a specific file inside all managed folders (LOGINSTANCE), accross both LOGROOT and LOGSTORAGE, extract and parse lines (LOGITEM) based on a specific pattern
  * Feature 3 will be about the parsing of LOGITEM and storing the relevant information.
  * Feature 4 will be to store in a local SQLite database, both all {LOGINSTANCE} already processed in a table, and all the information extracted from all the {LOGITEM} processed in another table.
  * Feature 5 will be to display in the main panel the most recent entry of parsed {LOGINSTANCE} 
  * Feature 6 will be to diplay in another panel to build multiple views on different values : a graph summing a count of each {MAP}, a timeline of {DCNAME}, {DCCODE} and {RAIDID} for the 5/10/15 (picker) most recent entries.


## ANNEX:

### Finding the correct LOGINSTANCE Files

 Example filename `2025.11.27_8-42-21_1.0.0.1.41967 application_000.log`


### Parsing LOGINSTANCE and extracting from each LOGITEM

LOGITEM Example : `2025-11-17 14:36:26.414|1.0.0.0.41787|Debug|application|TRACE-NetworkGameCreate profileStatus: 'Profileid: 5eacb6e52925b8162c347527, Status: Busy, RaidMode: Online, Ip: 74.119.145.115, Port: 17007, Location: bigmap, Sid: DE-FRM03G002_691b328afccd7c5c890fabd2_17.11.25_17-34-50, GameMode: deathmatch, shortId: NXPTFC'`

Finding the relevant LOGITEM from each LOGINSTANCE will be done by a simple search on the following pattern `Ip:`

The information to extract from a LOGITEM will be the key value pairs `NameInLog` {KEYNAME} > `Either the direct value or a process to refine the value(s)` : 
* Ip:{IPADDRESS} > '74.119.145.115'
* Location:{MAP} > 'bigmap'
* shortId:{RAIDID} > 'NXPTFC'
* Sid:{DCCODE} + {DCNAME} > 'DE-FRM03G002_691b328afccd7c5c890fabd2_17.11.25_17-34-50' which is then split on the '_' character so that only the first item 'DE-FRM03G002' is then further processed to output the following {DCCODE}:'03' and {DCNAME}:'DE-FRM'
* {DATE} > '2025-11-17 14:36:26.414' that is extracted by splitting the whole line on '|' and only saving the first item.
