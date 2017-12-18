using System;
using System.Collections;
using System.Text;
using System.Data;
using System.Data.Common;

namespace DateBaseConnectionPool
{
    /// <summary>
    /// ���ӳ�״̬
    /// </summary>
    public enum PoolState
    {
        /// <summary>
        /// �ոմ����Ķ��󣬱�ʾ�ö���δ�����ù�StartSeivice������
        /// </summary>
        UnInitialize,
        /// <summary>
        /// ��ʼ���У���״̬�·������ڰ��ղ�����ʼ�����ӳء�
        /// </summary>
        Initialize,
        /// <summary>
        /// ������
        /// </summary>
        Run,
        /// <summary>
        /// ֹͣ״̬
        /// </summary>
        Stop
    }

    /// <summary>
    /// Ҫ�������ӵļ���
    /// </summary>
    public enum ConnLevel
    {
        /// <summary>
        /// ��ռ��ʽ������ȫ�µ�������Դ�����Ҹ�������Դ�ڱ���ʹ���ͷŻ����ӳ�֮ǰ�����ڷ����ȥ��������ӳ�ֻ�ܷ������ü�������������Դ��ü��𽫲���һ���쳣����־���ӳ���Դ�ľ�
        /// </summary>
        ReadOnly,
        /// <summary>
        /// ���ȼ�-�ߣ�����ȫ�µ�������Դ����ʹ�����ü���������ע���˼��𲻱�֤�ڷ�����������Դ����Ȼ���ֶ���ռ����Դ���������ռ����Դ��ʹ��ReadOnely
        /// </summary>
        High,
        /// <summary>
        /// ���ȼ�-�У��ʵ�Ӧ�����ü���������������
        /// </summary>
        None,
        /// <summary>
        /// ���ȼ�-�ף�������ʹ�����ü���������������
        /// </summary>
        Bottom
    }

    /// <summary>
    /// ��������
    /// </summary>
    public enum ConnTypeEnum
    {
        /// <summary>
        /// ODBC ����Դ
        /// </summary>
        Odbc,
        /// <summary>
        /// OLE DB ����Դ
        /// </summary>
        OleDb,
        /// <summary>
        /// SqlServer ���ݿ�����
        /// </summary>
        SqlClient,
        /// <summary>
        /// Ĭ�ϣ��޷��䣩
        /// </summary>
        None
    }

    /// <summary>
    /// ���ӳ��е�һ����������
    /// </summary>
    public class ConnStruct : IDisposable
    {
        /// <summary>
        /// ���ӳ��е�����
        /// </summary>
        /// <param name="dbc">���ݿ�����</param>
        /// <param name="cte">��������</param>
        public ConnStruct(DbConnection dbc, ConnTypeEnum cte)
        {
            createTime = DateTime.Now;
            connect = dbc;
            connType = cte;
        }
        /// <summary>
        /// ���ӳ��е�����
        /// </summary>
        /// <param name="dt">���Ӵ���ʱ��</param>
        /// <param name="dbc">���ݿ�����</param>
        /// <param name="cte">��������</param>
        public ConnStruct(DbConnection dbc, ConnTypeEnum cte, DateTime dt)
        {
            createTime = dt;
            connect = dbc;
            connType = cte;
        }
        //--------------------------------------------------------------------
        private bool enable = true;//�Ƿ�ʧЧ
        private bool use = false;//�Ƿ����ڱ�ʹ����
        private bool allot = true;//��ʾ�������Ƿ���Ա�����
        private DateTime createTime = DateTime.Now;//����ʱ��
        private int useDegree = 0;//��ʹ�ô���
        private int repeatNow = 0;//��ǰ���ӱ��ظ����ö���
        private bool isRepeat = true;//�����Ƿ���Ա��ظ����ã����������ȥ�����ӿ���ʹ������ʱ�������Ա���ʶΪtrue
        private ConnTypeEnum connType = ConnTypeEnum.None;//��������
        private DbConnection connect = null;//���Ӷ���
        private object obj = null;//���Ӹ�������Ϣ

        #region ���Բ���
        /// <summary>
        /// ��ʾ�������Ƿ���Ա�����
        /// </summary>
        public bool Allot
        {
            get { return allot; }
            set { allot = value; }
        }
        /// <summary>
        /// �Ƿ�ʧЧ��false��ʾʧЧ��ֻ��
        /// </summary>
        public bool Enable
        { get { return enable; } }
        /// <summary>
        /// �Ƿ����ڱ�ʹ���У�ֻ��
        /// </summary>
        public bool IsUse
        { get { return use; } }
        /// <summary>
        /// ����ʱ�䣬ֻ��
        /// </summary>
        public DateTime CreateTime
        { get { return createTime; } }
        /// <summary>
        /// ��ʹ�ô�����ֻ��
        /// </summary>
        public int UseDegree
        { get { return useDegree; } }
        /// <summary>
        /// ��ǰ���ӱ��ظ����ö��٣�ֻ��
        /// </summary>
        public int RepeatNow
        { get { return repeatNow; } }
        /// <summary>
        /// �õ����ݿ�����״̬��ֻ��
        /// </summary>
        public ConnectionState State
        { get { return connect.State; } }
        /// <summary>
        /// �õ������ӣ�ֻ��
        /// </summary>
        public DbConnection Connection
        { get { return connect; } }
        /// <summary>
        /// �����Ƿ���Ա��ظ�����
        /// </summary>
        public bool IsRepeat
        {
            get { return isRepeat; }
            set { isRepeat = value; }
        }
        /// <summary>
        /// �������ͣ�ֻ��
        /// </summary> 
        public ConnTypeEnum ConnType
        { get { return connType; } }
        /// <summary>
        /// ���Ӹ�������Ϣ
        /// </summary>
        public object Obj
        {
            get { return obj; }
            set { obj = value; }
        }
        #endregion
        /// <summary>
        /// �����ݿ�����
        /// </summary>
        public void Open()
        { connect.Open(); }
        /// <summary>
        /// �ر����ݿ����� 
        /// </summary>
        public void Close()
        { connect.Close(); }
        /// <summary>
        /// ����������������ΪʧЧ
        /// </summary>
        public void SetConnectionLost()
        { enable = false; allot = false; }
        /// <summary>
        /// �������ȥ���̰߳�ȫ��
        /// </summary>
        public void Repeat()
        {
            lock (this)
            {
                if (enable == false)//���ӿ���
                    throw new ResLostnExecption();//������Դ�Ѿ�ʧЧ
                if (allot == false)//�Ƿ���Ա�����
                    throw new AllotExecption();//������Դ�����Ա�����
                if (use == true && isRepeat == false)
                    throw new AllotAndRepeatExecption();//������Դ�Ѿ������䲢�Ҳ������ظ�����
                repeatNow++;//���ü���+1
                useDegree++;//��ʹ�ô���+1
                use = true;//��ʹ��
            }
        }
        /// <summary>
        /// ���ͷŻ������̰߳�ȫ��
        /// </summary>
        public void Remove()
        {
            lock (this)
            {
                if (enable == false)//���ӿ���
                    throw new ResLostnExecption();//������Դ�Ѿ�ʧЧ
                if (repeatNow == 0)
                    throw new RepeatIsZeroExecption();//���ü����Ѿ�Ϊ0
                repeatNow--;//���ü���-1
                if (repeatNow == 0)
                    use = false;//δʹ��
                else
                    use = true;//ʹ����
            }
        }
        /// <summary>
        /// �ͷ���Դ
        /// </summary>
        public void Dispose()
        {
            enable = false;
            connect.Close();
            connect = null;
        }
    }
}
