using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime;
using System.Runtime.Serialization;
using System.Linq;

public abstract class DatabaseEntry
{
    [XmlIgnore]
	public ID DatabaseID { get; set; }

    [XmlIgnore]
    public Database Database { get; private set; }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	[XmlAttribute("ID")]
	public string _DatabaseIDSurrogate
	{
		get
		{
			return ID.IsNullOrNoID(DatabaseID) ? ID.NoID.ToString() : DatabaseID.ToString();
		}
		
		set
		{
			DatabaseID = ID.CreateID(value);
		}
	}
	
    public void PostLoad(Database db)
    {
        Database = db;

        OnPostLoad();
    }

	protected virtual void OnPostLoad() {}
}

public sealed class DatabaseEntryRef<T> where T : DatabaseEntry
{
    private ID id;
    private T cachedEntry;
    private bool entryHasBeenCached = false;

    private static DatabaseEntryRef<T> empty = new DatabaseEntryRef<T>();
    public static DatabaseEntryRef<T> Empty 
    {
        get
        {
            return empty;
        }
    }

    public bool IsValid
    {
        get
        {
            return !ID.IsNullOrNoID(id);
        }
    }

    public T Entry
    {
        get
        {
            if (!entryHasBeenCached)
            {
                CacheEntry();
                entryHasBeenCached = true;
            }

            return cachedEntry;
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [XmlText]
    public string _IDSurrogate
    {
        get
        {
            return ID.IsNullOrNoID(id) ? ID.NoID.ToString() : id.ToString();
        }
        
        set
        {
            id = ID.CreateID(value);
        }
    }

    public DatabaseEntryRef()
    {
        Database.Instance.OnFinishedLoading += FinishedLoadingHandler;
    }

    private void FinishedLoadingHandler()
    {
        CacheEntry();
        Database.Instance.OnFinishedLoading -= FinishedLoadingHandler;
    }

    public void CacheEntry()
    {
        entryHasBeenCached = true;

        if (ID.IsNullOrNoID(id))
        {
            cachedEntry = null;

            return;
        }

        cachedEntry = Database.Instance.GetEntry<T>(id);

        if (cachedEntry == null)
        {
            throw new System.Exception("Invalid Database Entry ID: " + id.ToString());
        }
    }

    public static implicit operator ID(DatabaseEntryRef<T> entryRef)
    {
        return entryRef.id;
    }

    public static implicit operator T(DatabaseEntryRef<T> entryRef)
    {
        return entryRef.Entry;
    }
}

internal class DatabaseTable : Dictionary<ID, DatabaseEntry> { }

public sealed class Database
{	
	private static Database instance;
	public static Database Instance
	{
		get
		{
			if (instance == null)
			{
				instance = new Database();
			}

			return instance;
		}
	}

    internal event System.Action OnFinishedLoading;

	private Dictionary<System.Type, DatabaseTable> tables = new Dictionary<System.Type, DatabaseTable>();

	public void ReadFiles(string rootPath)
	{
        Clear();

		Stack<string> dirs = new Stack<string>();
		
		dirs.Push(rootPath);
		while (dirs.Count > 0)
		{
			string currentDir = dirs.Pop();
			string[] subDirs = System.IO.Directory.GetDirectories(currentDir);
			string[] files = System.IO.Directory.GetFiles(currentDir);
			
			foreach (string file in files)
			{
				if (System.IO.Path.GetExtension(file).ToLower() == ".xml")
				{
					ReadDatabaseFile("file://" + file);
				}
			}
			
			if (subDirs != null && subDirs.Length > 0)
			{
				foreach (string dir in subDirs)
				{
					dirs.Push(dir);
				}
			}
		}

        OnFinishedLoading();

		PostLoad();
	}

    public void Clear()
    {
        tables = new Dictionary<System.Type, DatabaseTable>();
    }

	private void ReadDatabaseFile(string path)
	{
		using (XmlReader reader = XmlReader.Create(path))
		{
			while (reader.Read() && reader.MoveToContent() == XmlNodeType.Element)
			{
				if (reader.Name != "Database")
				{
					DatabaseEntry entry = (DatabaseEntry)DatabaseEntryFactory.Create(reader.Name, reader);
					if (entry != null)
					{
						AddEntry(entry);
					}
				}
			}
		}
	}
	
	private void PostLoad()
	{
		foreach (DatabaseTable table in tables.Values)
		{
			foreach (DatabaseEntry entry in table.Values)
			{
				entry.PostLoad(this);
			}
		}
	}
	
	private void AddEntry(DatabaseEntry entry)
	{
		DatabaseTable table;
		
		if (!tables.TryGetValue(entry.GetType(), out table))
		{
			Debug.Log("Created table for : " + entry.GetType().ToString());
			
			table = new DatabaseTable();
			
			tables.Add(entry.GetType(), table);
		}
		
		if (ID.IsNullOrNoID(entry.DatabaseID))
		{
			Debug.LogError("Database Entry has no ID: " + entry.GetType().ToString());
			return;
		}
		
		if (!table.ContainsKey(entry.DatabaseID))
		{
			table.Add(entry.DatabaseID, entry);
		}
		else
		{
			Debug.LogWarning("Duplicate database entry: " + entry.DatabaseID);
		}
	}
	
	public T GetEntry<T>(ID id) 
		where T : DatabaseEntry
	{
		DatabaseTable table;
		
		if (tables.TryGetValue(typeof(T), out table))
		{
			DatabaseEntry result;
			
			if (table.TryGetValue(id, out result))
			{
				return (T)result;
			}
		}
		
		return null;
	}
	
	public T GetEntry<T>(string idString)
		where T : DatabaseEntry
	{
		return GetEntry<T>(ID.GetID(idString));
	}
	
	public T[] GetEntries<T>()
		where T : DatabaseEntry
	{
		DatabaseTable table;
		
		if (tables.TryGetValue(typeof(T), out table))
		{
			return System.Linq.Enumerable.Cast<T>(table.Values).ToArray();
		}

		return null;
	}
}


