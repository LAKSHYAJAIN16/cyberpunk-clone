using System;
using System.Security;
using System.Collections.Generic;
using Kalos.Data.Containers;
using Kalos.Main;
using Kalos.Utilities.Interfaces;
using Kalos.Media.Calender.Main;
using Kalos.Media.Calender.Events;
using Kalos.Media.Calender.Objects;
using Kalos.Media.Calender.Serialization;
using Newtonsoft.Json;

namespace Kalos.Media.Calender
{
    namespace Main
    {
        [Serializable]
        public class Kalos_Calender : KCalender, KDestroyable, KCloneable
        {
            //Our User
            public User user;

            //Our Main Agent
            public Kalos_VirtualAssistant main;

            //Array and List of Events
            private List<CalenderEvent> EventsList = new List<CalenderEvent>();
            public CalenderEvent[] Events;

            //Static list of all callenders
            public static List<Kalos_Calender> calenders = new List<Kalos_Calender>();

            //Just SecurityData :L
            public string Name;
            public string Age;
            public int IAge;
            public string Gender;

            /// <summary>
            /// Constructor to create Calender
            /// </summary>
            public Kalos_Calender(User uv, Kalos_VirtualAssistant main)
            {
                this.user = uv;
                this.main = main;
                this.Name = uv.Name;
                this.Age = uv.Age;
                this.IAge = uv.IAge;
                this.Gender = uv.Gender;
            }

            /// <summary>
            /// Method to Add Event
            /// </summary>
            public void AddEvent(User uv, DateTime time, string name, string description, Kalos_VirtualAssistant main)
            {
                //If our user sent is different to our user defined, throw an error
                if (uv != user)
                {
                    throw new Exception("Error Encountered : User sent is different to user defined", new ArgumentException());
                    throw new CalenderExeption();
                }

                //Create new Event
                CalenderEvent cal = new CalenderEvent(name, description, time, uv);

                //Add it to our list, then convert the list to an array
                EventsList.Add(cal);
                Events = EventsList.ToArray();
            }

            /// <summary>
            /// Removes Event from calender
            /// </summary>
            public void RemoveEvent(User uv, DateTime time, string name, Kalos_VirtualAssistant main)
            {
                //If our user sent is different to our user defined, throw an error
                if (uv != user)
                {
                    throw new Exception("Error Encountered : User sent is different to user defined", new ArgumentException());
                    throw new CalenderExeption();
                }

                //Cycle through all the events and check if we have that event
                bool hasEvent = false;
                CalenderEvent Event = new CalenderEvent();
                Events = EventsList.ToArray();

                foreach (CalenderEvent eve in Events)
                {
                    //Check if our destined name is equal to the event name
                    bool isReqEvent = eve.EventName == name;

                    //if there are two events with the same name, throw an exception
                    if (hasEvent && isReqEvent)
                    {
                        throw new Exception("Error Encountered : Two events found with same name", new ArgumentException());
                        throw new CalenderExeption();
                    }

                    //Assign event as eve
                    Event = eve;

                    //Set bool to true
                    hasEvent = true;
                }

                //If we didn't find an event, throw exception
                if (!hasEvent)
                {
                    throw new Exception("Error Encountered : No event found with that name", new ArgumentException());
                    throw new CalenderExeption();
                }

                //If we did, remove that event from that list
                else if (hasEvent)
                {
                    EventsList.Remove(Event);
                    Events = EventsList.ToArray();
                }

            }

            /// <summary>
            /// Cycles through events
            /// Checks if their time is equal to the time currently
            /// </summary>
            public void CycleThroughEvents(Kalos_VirtualAssistant main)
            {
                //Gets current DateTime
                DateTime curtime = DateTime.Now;

                //Loops through
                foreach (CalenderEvent curevent in Events)
                {
                    //If the time is equal to the time now, trigger a event
                    if (curtime.Minute == curevent.EventTime.Minute)
                    {
                        UnityEngine.Debug.Log("BEEP BEEP! TIMER BOIZ!");
                        RemoveEvent(user, curtime, curevent.EventName, main);
                    }
                }
            }

            /// <summary>
            /// Verifies Sender
            /// </summary>
            public void VerifySender(object sender)
            {
                Type oType = sender.GetType();
                if (oType != main.GetType()){
                    throw new Exception("Error : Security Error. Request can only be sent by confirmed Data Type", new SecurityException());
                    throw new CalenderExeption();
                }
            }

            /// <summary>
            /// Destroys Instance
            /// </summary>
            public void Destroy(Kalos_VirtualAssistant main)
            {
                //Remove all values
                this.user = null;
                this.main = null;
                this.Events = null;
                this.EventsList = null;
            }

            /// <summary>
            /// Clones this Instance
            /// </summary>
            public object Clone(Kalos_VirtualAssistant main)
            {
                return this;
            }
        }
    }

    namespace Objects
    {
        public class CalenderExeption : Exception
        {
            public CalenderExeption()
            {

            }

            public CalenderExeption(string message)
            {
                UnityEngine.Debug.LogError(message);
            }

            public CalenderExeption(string message, bool isSevere)
            {
                if (!isSevere) UnityEngine.Debug.LogWarning(message);
                else
                    UnityEngine.Debug.LogError(message);
            }
        }

        public class CalenderSuccess : Success
        {
            public CalenderSuccess(string mes){
                base.SendMessage(mes);
            }
        }
    }

    namespace Events
    {
        [Serializable]
        public struct CalenderEvent
        {
            public string EventName;
            public string EventDescription;
            public DateTime EventTime;
            public User EventUser;

            public CalenderEvent(string name, string description, DateTime time, User user)
            {
                this.EventName = name;
                this.EventTime = time;
                this.EventUser = user;
                this.EventDescription = description;
            }
        }
    }

    namespace Serialization
    {
        public struct Kalos_Calender_Serialization
        {
            public static Kalos_Calender_Data ToCalenderDataArray(Kalos_Calender[] data)
            {
                try
                {
                    //Create empty KalosCalenderDataObject
                    Kalos_Calender_Data SerialzedData = new Kalos_Calender_Data();

                    //Loop through all the calenders
                    int userIndex = 0, eventIndex = 0;
                    foreach (Kalos_Calender cal in data)
                    {
                        //Indent our val
                        userIndex++;

                        //Add it to out list
                        SerialzedData.Add(cal);

                        //Loop through all the calenders events and add them
                        foreach (CalenderEvent calenderEvent in cal.Events)
                        {
                            SerialzedData.Add(calenderEvent);
                            eventIndex++;
                        }
                    }

                    //Assign parameters
                    SerialzedData.EventCount = eventIndex;
                    SerialzedData.CalenderCount = userIndex;

                    return SerialzedData;
                }

                catch(IndexOutOfRangeException a)
                {
                    UnityEngine.Debug.Log(string.Format("Error : {0} data, {1} target site, {2}, inner exeption", a.Data, a.TargetSite, a.InnerException));
                    return null;
                }
            }

            public static string ToJsonUnity(Kalos_Calender_Data data)
            {
                return UnityEngine.JsonUtility.ToJson(data);
            }

            public static string ToJsonNewtonsoft(Kalos_Calender_Data data)
            {
                return JsonConvert.SerializeObject(data);
            }

            public static Kalos_Calender_Data FromJsonUnity(string json)
            {
                return UnityEngine.JsonUtility.FromJson<Kalos_Calender_Data>(json);
            }

            public static Kalos_Calender_Data FromJsonNewtonsoft(string json)
            {
                return JsonConvert.DeserializeObject<Kalos_Calender_Data>(json);
            }

            public static Kalos_Calender[] FromCalenderDataArray(Kalos_Calender_Data j)
            {
                Kalos_Calender[] cals = j.CalenderList.ToArray();

                foreach (Kalos_Calender cal in cals){
                    User user = new User(cal.Name, cal.Age, cal.Gender);
                    cal.user = user;
                }

                return cals;
            }
        }

        public class Kalos_Calender_Data : KListableC, KListableE
        {
            public List<CalenderEvent> EventList = new List<CalenderEvent>();
            public List<Kalos_Calender> CalenderList = new List<Kalos_Calender>();

            public int EventCount { get; set; }

            public int CalenderCount { get; set; }

            public void Add(CalenderEvent e)
            {
                EventList.Add(e);
            }

            public void Add(Kalos_Calender e)
            {
                CalenderList.Add(e);
            }

            public void Sub(CalenderEvent e)
            {
                EventList.Remove(e);
            }

            public void Sub(Kalos_Calender e)
            {
                CalenderList.Remove(e);
            }
        }
    }

    namespace Creation.Management
    {
        public class Kalos_Calender_Manager : KInstanceable
        {
            public static Kalos_Calender_Manager Instance { get; set; }

            public static string SimpleToJSONUnity(Kalos_Calender[] data)
            {
                Kalos_Calender_Data c = Kalos_Calender_Serialization.ToCalenderDataArray(data);
                return Kalos_Calender_Serialization.ToJsonUnity(c);
            }

            public static Kalos_Calender[] SimpleFromJSONUnity(string json)
            {
                Kalos_Calender_Data g = Kalos_Calender_Serialization.FromJsonUnity(json);
                return Kalos_Calender_Serialization.FromCalenderDataArray(g);
            }

            public static string SimpleToJSONNewtonSoft(Kalos_Calender[] data)
            {
                Kalos_Calender_Data c = Kalos_Calender_Serialization.ToCalenderDataArray(data);
                return Kalos_Calender_Serialization.ToJsonNewtonsoft(c);
            }

            public static Kalos_Calender[] SimpleFromJSONNewtonSoft(string json)
            {
                Kalos_Calender_Data g = Kalos_Calender_Serialization.FromJsonNewtonsoft(json);
                return Kalos_Calender_Serialization.FromCalenderDataArray(g);
            }

            public void CreateInstance()
            {
                Instance = this;
            }

            public void DestroyInstance()
            {
                Instance = null;
            }
        }

        public enum CalenderDebugMode
        {
            NoDebug,
            Debug
        }
    }
}

namespace Kalos.Data.Containers
{
    public class User
    {
        public string Name { get; set; }

        public string Age { get; set; }

        public int IAge { get; set; }

        public string Gender { get; set; }

        public User(string name, string age, string gender)
        {
            this.Name = name;
            this.Age = age;
            this.IAge = int.Parse(age);
            this.Gender = gender;
        }
    }
}

namespace Kalos.Utilities.Interfaces
{
    public interface KDestroyable
    {
        public void Destroy(Kalos_VirtualAssistant main);
    }

    public interface KCloneable
    {
        public object Clone(Kalos_VirtualAssistant main);
    }

    public interface KMain
    {
        public void Speak(string text);
    }

    public interface KCalender
    {
        public void AddEvent(User uv, DateTime time, string name, string descrpition, Kalos_VirtualAssistant main);

        public void RemoveEvent(User uv, DateTime time, string name, Kalos_VirtualAssistant main);

        public void CycleThroughEvents(Kalos_VirtualAssistant main);

        public void VerifySender(object sender);
    }

    public interface KResponse
    {
        public void AssignMain(Kalos_VirtualAssistant main);
    }

    public interface KListableE
    {
        public void Add(CalenderEvent e);

        public void Sub(CalenderEvent e);
    }

    public interface KListableC
    {
        public void Add(Kalos_Calender e);

        public void Sub(Kalos_Calender e);
    }

    public interface KInstanceable
    {
        public void CreateInstance();

        public void DestroyInstance();
    }
}
