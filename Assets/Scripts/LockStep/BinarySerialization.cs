using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class BinarySerialization
{
	public static string SerializeObjectToString(object o) {
		byte[] byteArray = SerializeObjectToByteArray(o);
		if(byteArray != null) {
			return Convert.ToBase64String(byteArray);
		} else {
			return null;
		}
	}
	
	public static byte[] SerializeObjectToByteArray(object o) {
		if (!o.GetType().IsSerializable)
		{
			return null;
		}
		using (MemoryStream stream = new MemoryStream())
		{
			new BinaryFormatter().Serialize(stream, o);
			return stream.ToArray();
		}
	}
	
	public static object DeserializeObject(string str) {
		byte[] bytes = Convert.FromBase64String(str);
		return DeserializeObject(bytes);
	}
	
	public static object DeserializeObject(byte[] byteArray) {
		using (MemoryStream stream = new MemoryStream(byteArray))
		{
			return new BinaryFormatter().Deserialize(stream);
		}
	}
	
	public static T DeserializeObject<T>(byte[] byteArray) where T:class {
		object o = DeserializeObject(byteArray);
		return o as T;
	}
	
	public static T DeserializeObject<T>(string str) where T:class {
		object o = DeserializeObject(str);
		return o as T;
	}
}