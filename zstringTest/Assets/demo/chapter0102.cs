using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class chapter0102 : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    void OnGUI()
    {
        if (GUI.Button(new Rect(50, 50, 200, 50), "常量测试"))
        {
            string a = "hello world";
            string b = "hello world";
            string c = "hello " + "world";//+运算符连接的字面值
            string d = "hello ";
            string e = d + "world";//+运算符连接的非字面值
            Debug.Log(object.ReferenceEquals(a, b));//True         
            Debug.Log(object.ReferenceEquals(a,c));//True
            Debug.Log(object.ReferenceEquals(a, e));//False
            Debug.Log(a==e);//True
        }
        if (GUI.Button(new Rect(260, 50, 200, 50), "Intern测试"))
        {
            string a = "hello world";//已入池
            string d = "hello ";
            string e = "world"; 
            string f = d + e;//新对象
            string g = string.Intern(f);//指向池中
            Debug.Log(object.ReferenceEquals(a,f));//False
            Debug.Log(object.ReferenceEquals(a,g));//True

            string a2 = new string(new char[] { 'a', 'b', 'c' });
            string o2 = string.Copy(a2);
            Debug.Log(object.ReferenceEquals(o2, a));//False
            string.Intern(o2);//"abc"不在池中，放入池中返回自身
            Debug.Log(object.ReferenceEquals(o2, string.Intern(a2)));//True "abc"已在池中，返回池对象引用
            Debug.Log(object.ReferenceEquals(o2, a2));//False a2不在池中
        }
        if (GUI.Button(new Rect(50, 110, 200, 50), "IsInterned测试"))
        {
            string a2 = new string(new char[] { 'a', 'b', 'c' });
            string o2 = string.Copy(a2);
            Debug.Log(string.IsInterned(a2));
            string.Intern(o2);
            Debug.Log(string.IsInterned(o2));
        }
        if (GUI.Button(new Rect(260, 110, 200, 50), "GC测试"))
        {
            string a = "hello world";
            string b = "wo cao";
            for (int i = 0; i < 1000000; i++)
            {
                string c = a + b;
            }
        }
        if (GUI.Button(new Rect(50, 170, 200, 50), "指针操作测试"))
        {
            string s1 = "hello world";
            change(s1, 'x');//s1内容已经改变
            Debug.Log(s1);//打印：xxxxxxxxxxx 
            Debug.Log("hello world");//打印：xxxxxxxxxxx 
            Debug.Log(object.ReferenceEquals("hello world", s1));//True
        }
    }
        unsafe void change(string a, char b)
        {
            fixed (char* s = a)
            {
                for (int i = 0; i < a.Length; i++)
                {
                    s[i] = b;
                }
            }
        }
}
