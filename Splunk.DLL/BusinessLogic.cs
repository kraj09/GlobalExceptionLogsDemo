﻿using System;

namespace Splunk.DLL
{
    public class BusinessLogic
    {
        public string DoSomeBusiness()
        {
            return "I'm Done!";
        }

        public void RaiseException()
        {
            throw new Exception("Exception in Business Logic");
        }
    }
}
