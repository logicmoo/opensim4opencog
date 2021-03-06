// ------------------------------------------------------------------------------
// <auto-generated>
//    Generated by RoboKindChat.vshost.exe, version 0.9.0.0
//    Changes to this file may cause incorrect behavior and will be lost if code
//    is regenerated
// </auto-generated>
// ------------------------------------------------------------------------------
namespace org.robokind.avrogen.motion
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Avro;
	using Avro.Specific;
	
	public partial class RobotResponseHeaderRecord : ISpecificRecord, RobotResponseHeader
	{
		private static Schema _SCHEMA = Avro.Schema.Parse(@"{""type"":""record"",""name"":""RobotResponseHeaderRecord"",""namespace"":""org.robokind.avrogen.motion"",""fields"":[{""name"":""robotId"",""type"":""string""},{""name"":""requestSourceId"",""type"":""string""},{""name"":""requestDestinationId"",""type"":""string""},{""name"":""requestType"",""type"":""string""},{""name"":""requestTimestampMillisecUTC"",""type"":""long""},{""name"":""responseTimestampMillisecUTC"",""type"":""long""}]}");
		private string _robotId;
		private string _requestSourceId;
		private string _requestDestinationId;
		private string _requestType;
		private long _requestTimestampMillisecUTC;
		private long _responseTimestampMillisecUTC;
		public virtual Schema Schema
		{
			get
			{
				return RobotResponseHeaderRecord._SCHEMA;
			}
		}
		public string robotId
		{
			get
			{
				return this._robotId;
			}
			set
			{
				this._robotId = value;
			}
		}
		public string requestSourceId
		{
			get
			{
				return this._requestSourceId;
			}
			set
			{
				this._requestSourceId = value;
			}
		}
		public string requestDestinationId
		{
			get
			{
				return this._requestDestinationId;
			}
			set
			{
				this._requestDestinationId = value;
			}
		}
		public string requestType
		{
			get
			{
				return this._requestType;
			}
			set
			{
				this._requestType = value;
			}
		}
		public long requestTimestampMillisecUTC
		{
			get
			{
				return this._requestTimestampMillisecUTC;
			}
			set
			{
				this._requestTimestampMillisecUTC = value;
			}
		}
		public long responseTimestampMillisecUTC
		{
			get
			{
				return this._responseTimestampMillisecUTC;
			}
			set
			{
				this._responseTimestampMillisecUTC = value;
			}
		}
		public virtual object Get(int fieldPos)
		{
			switch (fieldPos)
			{
			case 0: return this.robotId;
			case 1: return this.requestSourceId;
			case 2: return this.requestDestinationId;
			case 3: return this.requestType;
			case 4: return this.requestTimestampMillisecUTC;
			case 5: return this.responseTimestampMillisecUTC;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
			};
		}
		public virtual void Put(int fieldPos, object fieldValue)
		{
			switch (fieldPos)
			{
			case 0: this.robotId = (System.String)fieldValue; break;
			case 1: this.requestSourceId = (System.String)fieldValue; break;
			case 2: this.requestDestinationId = (System.String)fieldValue; break;
			case 3: this.requestType = (System.String)fieldValue; break;
			case 4: this.requestTimestampMillisecUTC = (System.Int64)fieldValue; break;
			case 5: this.responseTimestampMillisecUTC = (System.Int64)fieldValue; break;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
			};
		}
	}
}
