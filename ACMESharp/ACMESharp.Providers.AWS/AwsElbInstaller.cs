﻿using ACMESharp.Installer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.PKI;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancing.Model;

namespace ACMESharp.Providers.AWS
{
    public class AwsElbInstaller : IInstaller
    {
        public static readonly IEnumerable<string> VALID_LIS_PROTOS = new[]
        {
            "HTTPS",
            "SSL",
        };

        public static readonly IEnumerable<string> VALID_INST_PROTOS = new[]
        {
            "HTTPS",
            "SSL",
            "HTTP",
            "TCP",
        };

        public string LoadBalancerName
        { get; set; }

        public int LoadBalancerPort
        { get; set; }

        public string LoadBalancerProtocol
        { get; set; }

        public int InstancePort
        { get; set; }

        public string InstanceProtocol
        { get; set; }

        public string ExistingServerCertificateName
        { get; set; }

        public AwsIamCertificateInstaller CertInstaller
        { get; set; }

        public AwsCommonParams CommonParams
        { get; set; }

        public bool IsDisposed
        { get; private set; }

        public void Install(PrivateKey pk, Crt crt, IEnumerable<PKI.Crt> chain,
                CertificateProvider cp)
        {
            AssertNotDisposed();

            if (CertInstaller != null)
            {
                CertInstaller.Install(pk, crt, chain, cp);
                ExistingServerCertificateName = CertInstaller.ServerCertificateName;
            }

            string certArn;
            using (var client = new AmazonIdentityManagementServiceClient(
                CommonParams.ResolveCredentials(),
                CommonParams.RegionEndpoint))
            {
                var iamRequ = new GetServerCertificateRequest
                {
                    ServerCertificateName = ExistingServerCertificateName,
                };

                var iamResp = client.GetServerCertificate(iamRequ);
                certArn = iamResp?.ServerCertificate?.ServerCertificateMetadata?.Arn;
            }

            if (string.IsNullOrEmpty(certArn))
                throw new InvalidOperationException("unable to resolve server certificate again IAM store");

            using (var client = new AmazonElasticLoadBalancingClient(
                CommonParams.ResolveCredentials(),
                CommonParams.RegionEndpoint))
            {
                if (!string.IsNullOrEmpty(LoadBalancerProtocol))
                {
                    var iamRequ = new CreateLoadBalancerListenersRequest
                    {
                        LoadBalancerName = this.LoadBalancerName,
                        Listeners = new List<Listener>
                        {
                            new Listener
                            {
                                LoadBalancerPort = this.LoadBalancerPort,
                                Protocol = this.LoadBalancerProtocol,
                                InstancePort = this.InstancePort,
                                InstanceProtocol = this.InstanceProtocol,
                                SSLCertificateId = certArn,
                            }
                        }
                    };

                    var iamResp = client.CreateLoadBalancerListeners(iamRequ);
                    // TODO:  any checks we should do?
                }
                else
                {
                    var iamRequ = new SetLoadBalancerListenerSSLCertificateRequest
                    {
                        LoadBalancerName = this.LoadBalancerName,
                        LoadBalancerPort = this.LoadBalancerPort,
                        SSLCertificateId = certArn,
                    };

                    var iamResp = client.SetLoadBalancerListenerSSLCertificate(iamRequ);
                    // TODO:  any checks we should do?
                }
            }
        }

        public void Uninstall(PrivateKey pk, Crt crt, IEnumerable<PKI.Crt> chain,
                CertificateProvider cp)
        {
            AssertNotDisposed();

            throw new NotImplementedException();
        }

        private void AssertNotDisposed()
        {
            if (IsDisposed)
                throw new InvalidOperationException("AWS Challenge Handler is disposed");
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (CertInstaller != null)
                    {
                        CertInstaller.Dispose();
                        CertInstaller = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                IsDisposed = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AwsElbInstaller() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
