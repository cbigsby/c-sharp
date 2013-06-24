using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.ComponentModel;
using System.Threading;
using System.Collections;
using PubNubMessaging.Core;
#if (USE_JSONFX)
using JsonFx.Json;
#elif (USE_DOTNET_SERIALIZATION)
using System.Runtime.Serialization.Json;
using System.Web.Script.Serialization;
#else
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif
namespace PubNubMessaging.Tests
{
    public class WhenAClientIsPresented: UUnitTestCase
    {
        /*ManualResetEvent subscribeManualEvent = new ManualResetEvent(false);
        ManualResetEvent presenceManualEvent = new ManualResetEvent(false);
        ManualResetEvent unsubscribeManualEvent = new ManualResetEvent(false);

        ManualResetEvent subscribeUUIDManualEvent = new ManualResetEvent(false);
        ManualResetEvent presenceUUIDManualEvent = new ManualResetEvent(false);
        ManualResetEvent unsubscribeUUIDManualEvent = new ManualResetEvent(false);

        ManualResetEvent hereNowManualEvent = new ManualResetEvent(false);
        //ManualResetEvent presenceUnsubscribeEvent = new ManualResetEvent(false);
        //ManualResetEvent presenceUnsubscribeUUIDEvent = new ManualResetEvent(false);

        static bool receivedPresenceMessage = false;
        static bool receivedHereNowMessage = false;
        static bool receivedCustomUUID = false;*/

        string customUUID = "mylocalmachine.mydomain.com";

        [UUnitTest]
        public void ThenPresenceShouldReturnReceivedMessage()
        {
			Debug.Log("Running ThenPresenceShouldReturnReceivedMessage()");
			Pubnub pubnub = new Pubnub(
				"demo",
				"demo",
				"",
				"",
				false
				);
			string channel = "hello_world";
			Common commonPresence = new Common();
			commonPresence.DeliveryStatus = false;
			commonPresence.Response = null;
			
			pubnub.PubnubUnitTest = commonPresence.CreateUnitTestInstance("WhenAClientIsPresented", "ThenPresenceShouldReturnReceivedMessage");
			
			pubnub.Presence(channel, commonPresence.DisplayReturnMessageDummy, commonPresence.DisplayReturnMessageDummy, commonPresence.DisplayReturnMessageDummy);
			
			Common commonSubscribe = new Common();
			commonSubscribe.DeliveryStatus = false;
			commonSubscribe.Response = null;
			
			pubnub.Subscribe(channel, commonSubscribe.DisplayReturnMessage, commonSubscribe.DisplayReturnMessageDummy, commonPresence.DisplayReturnMessageDummy);
			while (!commonSubscribe.DeliveryStatus) ;
			
			string response = "";
			if (commonSubscribe.Response == null) {
				Debug.Log("Null response");
				UUnitAssert.Fail();
			}
			else
			{
				IList<object> responseFields = commonSubscribe.Response as IList<object>;
				foreach (object item in responseFields)
				{
					response = item.ToString();
					Console.WriteLine("Response:" + response);
					//Assert.IsNotEmpty(strResponse);
				}
				bool result = "hello_world".Equals(responseFields[2]);
				UUnitAssert.True(result);
				Debug.Log("ThenPresenceShouldReturnReceivedMessage: " + result.ToString());
			}
        }

        [UUnitTest]
		public void ThenPresenceShouldReturnCustomUUID ()
		{
			Pubnub pubnub = new Pubnub("demo", "demo", "", "", false);
			
			Common commonHereNow = new Common();
			commonHereNow.DeliveryStatus = false;
			commonHereNow.Response = null;
			
			Common commonSubscribe = new Common();
			commonSubscribe.DeliveryStatus = false;
			commonSubscribe.Response = null;
			
			pubnub.PubnubUnitTest = commonHereNow.CreateUnitTestInstance("WhenAClientIsPresented", "ThenPresenceShouldReturnCustomUUID");;
			pubnub.SessionUUID = "CustomSessionUUIDTest";
			
			string channel = "hello_world";
			
			pubnub.Subscribe(channel, commonSubscribe.DisplayReturnMessageDummy, commonSubscribe.DisplayReturnMessage, commonSubscribe.DisplayReturnMessage);
			
			while (!commonSubscribe.DeliveryStatus);
			
			pubnub.HereNow<string>(channel, commonHereNow.DisplayReturnMessage, commonHereNow.DisplayReturnMessage);
			
			while (!commonHereNow.DeliveryStatus);
			if (commonHereNow.Response!= null)
			{
#if (USE_JSONFX)
				IList<object> fields = new JsonFXDotNet ().DeserializeToObject (commonHereNow.Response.ToString ()) as IList<object>;
				if (fields [0] != null)
				{
					bool result = false;
					Dictionary<string, object> message = (Dictionary<string, object>)fields [0];
					foreach (KeyValuePair<String, object> entry in message)
					{
						Console.WriteLine("value:" + entry.Value + "  " + "key:" + entry.Key);
						Type valueType = entry.Value.GetType();
						var expectedType = typeof(string[]);
						if (valueType.IsArray && expectedType.IsAssignableFrom(valueType))
						{
							List<string> uuids = new List<string>(entry.Value as string[]);
							if(uuids.Contains(pubnub.SessionUUID )){
								result= true;
								break;
							}
						}
					}
					UUnitAssert.True(result);
					Debug.Log("ThenPresenceShouldReturnCustomUUID: " + result.ToString());
				} 
				else
				{
					Debug.Log("Null response");
					UUnitAssert.Fail();
				}
#else
				object[] serializedMessage = JsonConvert.DeserializeObject<object[]>(commonHereNow.Response.ToString());
				JContainer dictionary = serializedMessage[0] as JContainer;
				var uuid = dictionary["uuids"].ToString();
				if (uuid != null)
				{
					Assert.True(uuid.Contains(pubnub.SessionUUID));
				} else {
					Assert.Fail("Custom uuid not found.");
				}
#endif
			} else {
				Debug.Log("Null response");
				UUnitAssert.Fail();
			}
			
		}

        /*[UUnitTest]
        public void IfHereNowIsCalledThenItShouldReturnInfo()
        {
			Debug.Log("Running IfHereNowIsCalledThenItShouldReturnInfo()");
            receivedHereNowMessage = false;

            Pubnub pubnub = new Pubnub("demo", "demo", "", "", false);
			pubnub.JsonPluggableLibrary = new JsonFXDotNet();
			
            PubnubUnitTest unitTest = new PubnubUnitTest();
            unitTest.TestClassName = "WhenAClientIsPresented";
            unitTest.TestCaseName = "IfHereNowIsCalledThenItShouldReturnInfo";
            pubnub.PubnubUnitTest = unitTest;
            string channel = "my/channel";
            pubnub.HereNow<string>(channel, ThenHereNowShouldReturnMessage, DummyErrorCallback);
            hereNowManualEvent.WaitOne();
            UUnitAssert.True(receivedHereNowMessage, "here_now message not received");
        }*/

		[UUnitTest]
		public void IfHereNowIsCalledThenItShouldReturnInfo()
		{
			Pubnub pubnub = new Pubnub(
				"demo",
				"demo",
				"",
				"",
				false
				);
			Common common = new Common();
			common.DeliveryStatus = false;
			common.Response = null;
			
			HereNow(pubnub, "IfHereNowIsCalledThenItShouldReturnInfo", common.DisplayReturnMessage);
			while (!common.DeliveryStatus) ;
			
			ParseResponse(common.Response);
		}
		
		void HereNow(Pubnub pubnub, string unitTestCaseName, 
		             Action<object> userCallback)
		{
			string channel = "hello_world";
			
			PubnubUnitTest unitTest = new PubnubUnitTest();
			unitTest.TestClassName = "WhenAClientIsPresented";
			unitTest.TestCaseName = unitTestCaseName;
			pubnub.PubnubUnitTest = unitTest;
			
			pubnub.HereNow(channel, userCallback, userCallback);
		}
		
		public void ParseResponse(object commonResponse)
		{
			string response = "";
			if (commonResponse.Equals (null)) {
				Debug.Log("Null response");
				UUnitAssert.Fail();
			}
			else
			{
				IList<object> responseFields = commonResponse as IList<object>;
				foreach(object item in responseFields)
				{
					response = item.ToString();
					Console.WriteLine("Response:" + response);
					bool result = !string.IsNullOrEmpty(response);
					UUnitAssert.True(result);
				}
				Dictionary<string, object> message = (Dictionary<string, object>)responseFields[0];
				foreach(KeyValuePair<String, object> entry in message)
				{
					Console.WriteLine("value:" + entry.Value + "  " + "key:" + entry.Key);
				}
				
				/*object[] objUuid = (object[])message["uuids"];
                    foreach (object obj in objUuid)
                    {
                        Console.WriteLine(obj.ToString()); 
                    }*/
				//Assert.AreNotEqual(0, message["occupancy"]);
			}
		}
		
		[UUnitTest]
		public void IfHereNowIsCalledWithCipherThenItShouldReturnInfo()
		{
			Pubnub pubnub = new Pubnub(
				"demo",
				"demo",
				"",
				"enigma",
				false
				);
			Common common = new Common();
			common.DeliveryStatus = false;
			common.Response = null;
			
			HereNow(pubnub, "IfHereNowIsCalledThenItShouldReturnInfo", common.DisplayReturnMessage);
			while (!common.DeliveryStatus) ;
			
			ParseResponse(common.Response);
		}
    }
}
