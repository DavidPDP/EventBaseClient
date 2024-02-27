using Avro;
using Avro.Specific;

namespace EventBaseClient.Test;

public partial class EventRecord : ISpecificRecord
{
    public static Schema _SCHEMA = Schema.Parse(@"{""type"":""record"",""name"":""EventRecord"",""fields"":[{""name"":""name"",""type"":""string""},{""name"":""timestamp"",""type"":""long""}]}");
    private string _name;
    private long _timestamp;

    public virtual Schema Schema
    {
        get
        {
            return User._SCHEMA;
        }
    }
    public string name
    {
        get
        {
            return this._name;
        }
        set
        {
            this._name = value;
        }
    }
    public long timestamp
    {
        get
        {
            return this._timestamp;
        }
        set
        {
            this._timestamp = value;
        }
    }
    
    public virtual object Get(int fieldPos)
    {
        switch (fieldPos)
        {
            case 0: return this.name;
            case 1: return this.timestamp;
            default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
        };
    }
    public virtual void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: this.name = (System.String)fieldValue; break;
            case 1: this.timestamp = (System.Int64)fieldValue; break;
            default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
        };
    }
}
