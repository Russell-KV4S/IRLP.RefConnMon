using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace KV4S.AmateurRadio.IRLP.RefConnMon
{
    public class Node
    {
        private string m_Callsign;
        public string Callsign
        {
            get { return m_Callsign; }
            set { m_Callsign = value; }
        }

        private string m_Number;
        public string Number
        {
            get { return m_Number; }
            set { m_Number = value; }
        }

        private string m_ConnectedReflector;
        public string ConnectedReflector
        {
            get { return m_ConnectedReflector; }
            set { m_ConnectedReflector = value; }
        }

    }
    public class NodeCollection : BindingList<Node>
    {
        public NodeCollection() { }
    }
}
