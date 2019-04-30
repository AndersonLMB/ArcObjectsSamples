using System;
using ESRI.ArcGIS;
using ESRI.ArcGIS.esriSystem;

namespace Sample1
{
    public class ArcEngineLicense : IDisposable
    {

        public enum LicenseType : byte
        {
            ENGINE = 1,
            DESKTOP = 2
        }

        #region 成员变量
        private static IAoInitialize m_pAoInit;
        private static byte m_extensionCode = 0;
        private static readonly object _mutexLocker = new object();
        #endregion

        #region 构造和析构函数

        public ArcEngineLicense()
        {
            lock (_mutexLocker)
            {
                RegisterLicenseService((byte)(LicenseType.ENGINE | LicenseType.DESKTOP));
            }
        }

        public ArcEngineLicense(byte licenseType)
        {
            lock (_mutexLocker)
            {

                // 如果是ArcGIS 10，不同产品类型之间License相互不通用
                RegisterLicenseService(licenseType);

            }
        }

        public ArcEngineLicense(byte licenseType, byte extensionCode)
        {
            lock (_mutexLocker)
            {
                RegisterLicenseService(licenseType);

                if (extensionCode > 0)
                {
                    m_extensionCode = extensionCode;
                    m_pAoInit.CheckOutExtension((esriLicenseExtensionCode)m_extensionCode);
                }
            }
        }

        ~ArcEngineLicense()
        {
            Dispose();
        }

        #endregion

        #region IDisposable 成员

        public void Dispose()
        {
            lock (_mutexLocker)
            {
                if (m_pAoInit == null) return;

                if (m_extensionCode > 0 &&
                    m_pAoInit.IsExtensionCheckedOut((esriLicenseExtensionCode)m_extensionCode))
                    m_pAoInit.CheckInExtension((esriLicenseExtensionCode)m_extensionCode);
                UnRegisterLicenseService();
                m_pAoInit = null;
            }
        }

        #endregion

        #region 私有方法

        private void RegisterLicenseService(byte licenseType)
        {
            bool bindSuccess = (licenseType == 1) ? RuntimeManager.Bind(ProductCode.Engine) :
                ((licenseType == 2) ? RuntimeManager.Bind(ProductCode.Desktop) :
                RuntimeManager.Bind(ProductCode.EngineOrDesktop));
            if (bindSuccess == false)
                throw new Exception("不能绑定到ArcGIS产品！");

            if (m_pAoInit != null)
            {
                if (m_extensionCode > 0 &&
                    m_pAoInit.IsExtensionCheckedOut((esriLicenseExtensionCode)m_extensionCode))
                    m_pAoInit.CheckInExtension((esriLicenseExtensionCode)m_extensionCode);
                UnRegisterLicenseService();
                m_pAoInit = null;
            }
            m_pAoInit = new AoInitializeClass();
            bool enabled;

            // ArcGIS 10以下版本不区分ArcEngine或Desktop，10以上区分产品

            enabled = (RuntimeManager.ActiveRuntime.Product == ProductCode.Engine);
            if (enabled && (InitializeProduct(esriLicenseProductCode.esriLicenseProductCodeAdvanced) ||
                InitializeProduct(esriLicenseProductCode.esriLicenseProductCodeEngineGeoDB)))
                return;

            enabled = (RuntimeManager.ActiveRuntime.Product == ProductCode.Desktop);
            if (enabled && (InitializeProduct(esriLicenseProductCode.esriLicenseProductCodeAdvanced) ||
            InitializeProduct(esriLicenseProductCode.esriLicenseProductCodeStandard) ||
            InitializeProduct(esriLicenseProductCode.esriLicenseProductCodeBasic)))
                return;

            throw new System.Exception("ArcGIS License Cannot Checkout");
        }

        private bool InitializeProduct(esriLicenseProductCode productCode)
        {
            esriLicenseStatus status = m_pAoInit.IsProductCodeAvailable(productCode);
            if (status == esriLicenseStatus.esriLicenseAvailable)
            {
                status = m_pAoInit.Initialize(productCode);
                if (status == esriLicenseStatus.esriLicenseCheckedOut ||
                    status == esriLicenseStatus.esriLicenseAlreadyInitialized)
                    return true;
            }
            return false;
        }

        private void UnRegisterLicenseService()
        {
            if (m_pAoInit != null)
            {
                try
                {
                    m_pAoInit.Shutdown();
                }
                catch
                {
                }
                finally
                {
                    //ComObject.Release(ref m_pAoInit);
                }
            }
        }

        #endregion
    }
}
