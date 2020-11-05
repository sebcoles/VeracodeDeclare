﻿using Microsoft.Extensions.Configuration;using Microsoft.Extensions.Options;using Newtonsoft.Json;using NUnit.Framework;using System;using System.Collections.Generic;using System.IO;using System.Linq;using Veracode.OSS.Declare.Logic;
using Veracode.OSS.Declare.Shared;
using Veracode.OSS.Wrapper;using Veracode.OSS.Wrapper.Configuration;using Veracode.OSS.Wrapper.Models;namespace Veracode.OSS.Declare.Tests.Integration{    [TestFixture]    [Category("Integration")]    public class DscIntegrationTests    {        private VeracodeConfiguration veracodeConfig = new VeracodeConfiguration();        private IDscLogic _dscLogic;        private IVeracodeService _veracodeService;        private IVeracodeRepository _veracodeRepository;        private List<ApplicationProfile> application_profiles;        private Random _rand = new Random();        [OneTimeSetUp]        public void Setup()        {
            IConfiguration Configuration = new ConfigurationBuilder()
.SetBasePath(Directory.GetCurrentDirectory())
#if DEBUG
                .AddJsonFile($"appsettings.Development.json", false)
#else
                .AddJsonFile("appsettings.json", false)
#endif
                .Build();
            
            var options = Options.Create(
                VeracodeFileHelper.GetConfiguration(
                    Configuration.GetValue<string>("VeracodeFileLocation")));            _veracodeRepository = new VeracodeRepository(options);            _veracodeService = new VeracodeService(_veracodeRepository);            _dscLogic = new DscLogic(_veracodeService, _veracodeRepository);        }        private void SetupIncompleteConfiguration()        {            application_profiles = JsonConvert.DeserializeObject<JsonConfig>(                System.IO.File.ReadAllText("veracode.incomplete.json")).application_profiles;        }        private void SetupCompleteConfiguration()        {            application_profiles = JsonConvert.DeserializeObject<JsonConfig>(                System.IO.File.ReadAllText("veracode.complete.json")).application_profiles;        }        public void TearDownFromConfiguration()        {            foreach (var app in application_profiles)            {                var retrievedApp = _veracodeRepository.GetAllApps().Where(x => x.app_name == app.application_name).SingleOrDefault();                if (retrievedApp != null)                {                    _veracodeRepository.DeleteApp(new ApplicationType                    {                        app_id = retrievedApp.app_id                    });                    retrievedApp = _veracodeRepository.GetAllApps().Where(x => x.app_name == app.application_name).SingleOrDefault();                    Assert.IsNull(retrievedApp);                }

                var retrievedPolicy = _veracodeRepository.GetPolicies().Where(x => x.name == app.application_name).SingleOrDefault();                if (retrievedPolicy != null)                {                    _veracodeRepository.DeletePolicy(retrievedPolicy.guid);                    retrievedPolicy = _veracodeRepository.GetPolicies().Where(x => x.name == app.application_name).SingleOrDefault();                    Assert.IsNull(retrievedApp);                }                var retrievedTeam = _veracodeRepository.GetTeams().Where(x => x.team_name == app.application_name).SingleOrDefault();                if (retrievedTeam != null)                {                    _veracodeRepository.DeleteTeam(retrievedTeam.team_id);                    retrievedTeam = _veracodeRepository.GetTeams().Where(x => x.team_name == app.application_name).SingleOrDefault();                    Assert.IsNull(retrievedTeam);                }

                foreach (var user in app.users)                {                    var retrievedUser = _veracodeRepository.GetUsers().Where(x => x == user.email_address).SingleOrDefault();                    if (retrievedUser != null)                    {                        _veracodeRepository.DeleteUser(user.email_address);                        retrievedUser = _veracodeRepository.GetUsers().Where(x => x == user.email_address).SingleOrDefault();                        Assert.IsNull(retrievedUser);                    }                }            }        }        public void SetupFromConfiguration()        {            foreach (var app in application_profiles)            {
                _dscLogic.MakeItSoPolicy(app, app.policy);                _dscLogic.MakeItSoApp(app);                _dscLogic.MakeItSoTeam(app);                foreach (var user in app.users)                {                    user.teams = app.application_name;                    user.email_address = _rand.Next(1000000).ToString() + user.email_address;                    _dscLogic.MakeItSoUser(user, app);                }                Assert.IsTrue(_veracodeService.DoesAppExist(app));                Assert.IsTrue(_veracodeService.DoesPolicyExist(app));                Assert.IsTrue(_veracodeService.DoesTeamExistForApp(app));                foreach (var user in app.users)                    Assert.IsTrue(_veracodeService.DoesUserExist(user));            }        }        [Test]        public void Test_IncompleteConfiguration_ModuleConfiguration()        {            SetupIncompleteConfiguration();            TearDownFromConfiguration();            SetupFromConfiguration();            var results = new List<KeyValuePair<string, bool>>();            foreach (var app in application_profiles)            {                var doesScanConfirm = _dscLogic.ConformConfiguration(app,                        app.files.ToArray(),                        app.modules.ToArray(), true);                results.Add(new KeyValuePair<string, bool>(                    app.application_name,                    doesScanConfirm                ));            }            foreach (var summary in results)            {                var message = summary.Value ? "DOES" : "DOES NOT";                Console.Write($"Application {summary.Key} scan config {message} conform.");                Assert.IsFalse(summary.Value);            }            TearDownFromConfiguration();        }        [Test]        public void Test_CompleteConfiguration_ModuleConfiguration()        {            SetupCompleteConfiguration();            TearDownFromConfiguration();            SetupFromConfiguration();            var results = new List<KeyValuePair<string, bool>>();            foreach (var app in application_profiles)            {                var doesScanConfirm = _dscLogic.ConformConfiguration(app,                        app.files.ToArray(),                        app.modules.ToArray(), true);                results.Add(new KeyValuePair<string, bool>(                    app.application_name,                    doesScanConfirm                ));            }            foreach (var summary in results)            {                var message = summary.Value ? "DOES" : "DOES NOT";                Console.Write($"Application {summary.Key} scan config {message} conform.");                Assert.IsTrue(summary.Value);            }            TearDownFromConfiguration();        }        [Test]        public void Test_CompleteConfiguration_FullScan_ApplyMitigations()        {            SetupCompleteConfiguration();            TearDownFromConfiguration();            SetupFromConfiguration();            foreach (var app in application_profiles)
            {
                _dscLogic.MakeItSoScan(app, app.files.ToArray(), app.modules.ToArray());
                _dscLogic.MakeItSoMitigations(app);
            }
            TearDownFromConfiguration();        }        [Test]        public void MakeItSoApp_ShouldCreateApp()        {            SetupIncompleteConfiguration();            var app = application_profiles.First();            var policy_name = $"{app.application_name}-MakeItSoAppTest";            var app_name = $"{app.application_name}-MakeItSoAppTest";            app.application_name = app_name;            app.policy.name = policy_name;            _dscLogic.MakeItSoPolicy(app, app.policy);            _dscLogic.MakeItSoApp(app);            Assert.IsTrue(_veracodeService.DoesAppExist(app));            var retrievedApp = _veracodeRepository.GetAllApps().Where(x => x.app_name == app_name).Single();            _veracodeRepository.DeleteApp(new ApplicationType            {                app_id = retrievedApp.app_id            });            retrievedApp = _veracodeRepository.GetAllApps().Where(x => x.app_name == app.application_name).SingleOrDefault();            Assert.IsNull(retrievedApp);            var retrievedPolicy = _veracodeRepository.GetPolicies().Where(x => x.name == policy_name).Single();            _veracodeRepository.DeletePolicy(retrievedPolicy.guid);        }        [Test]        public void MakeItSoApp_ShouldUpdateApp()        {            SetupIncompleteConfiguration();            var app = application_profiles.First();            var app_name = $"{app.application_name}-MakeItSoUpdateAppTest";            app.application_name = app_name;            _dscLogic.MakeItSoApp(app);            Assert.IsTrue(_veracodeService.DoesAppExist(app));            app.business_owner = "Updated Business Owner";            _dscLogic.MakeItSoApp(app);            var retrievedApp = _veracodeRepository.GetAllApps().Where(x => x.app_name == app_name).Single();            var appDetail = _veracodeRepository.GetAppDetail($"{retrievedApp.app_id}");            Assert.AreEqual("Updated Business Owner", appDetail.application[0].business_owner);

            _veracodeRepository.DeleteApp(new ApplicationType            {                app_id = retrievedApp.app_id            });            retrievedApp = _veracodeRepository.GetAllApps().Where(x => x.app_name == app.application_name).SingleOrDefault();            Assert.IsNull(retrievedApp);        }        [Test]        public void MakeItSoPolicy_ShouldCreatePolicy()        {            SetupIncompleteConfiguration();            var app = application_profiles.First();            var app_name = $"{app.application_name}-MakeItSoPolicyTest";            var policy_name = $"{app.application_name}-MakeItSoPolicyTest";            app.application_name = app_name;            _dscLogic.MakeItSoPolicy(app, app.policy);            Assert.IsTrue(_veracodeService.DoesPolicyExist(app));            var retrievedPolicy = _veracodeRepository.GetPolicies().Where(x => x.name == policy_name).Single();            _veracodeRepository.DeletePolicy(retrievedPolicy.guid);            retrievedPolicy = _veracodeRepository.GetPolicies().Where(x => x.name == policy_name).SingleOrDefault();            Assert.IsNull(retrievedPolicy);        }        [Test]        public void MakeItSoPolicy_ShouldUpdatePolicy()        {            SetupIncompleteConfiguration();            var app = application_profiles.First();            var app_name = $"{app.application_name}-MakeItSoPolicyTest";            var policy_name = $"{app.application_name}-MakeItSoPolicyTest";            app.application_name = app_name;            _dscLogic.MakeItSoPolicy(app, app.policy);            Assert.IsTrue(_veracodeService.DoesPolicyExist(app));            app.policy.description = "Jammy Jam Jam";            _dscLogic.MakeItSoPolicy(app, app.policy);            var retrievedPolicy = _veracodeRepository.GetPolicies().Where(x => x.name == policy_name).Single();            Assert.AreEqual("Jammy Jam Jam", retrievedPolicy.description);

            _veracodeRepository.DeletePolicy(retrievedPolicy.guid);            retrievedPolicy = _veracodeRepository.GetPolicies().Where(x => x.name == policy_name).SingleOrDefault();            Assert.IsNull(retrievedPolicy);        }        [Test]        public void MakeItSoTeam_ShouldCreateTeam()        {            SetupIncompleteConfiguration();            var app = application_profiles.First();            var app_name = $"{app.application_name}-MakeItSoTeamTest";            var team_name = $"{app.application_name}-MakeItSoTeamTest";            app.application_name = app_name;            _dscLogic.MakeItSoTeam(app);            Assert.IsTrue(_veracodeService.DoesTeamExistForApp(app));            var retrieved = _veracodeRepository.GetTeams().Where(x => x.team_name == team_name).Single();            _veracodeRepository.DeleteTeam(retrieved.team_id);            retrieved = _veracodeRepository.GetTeams().Where(x => x.team_name == team_name).SingleOrDefault();            Assert.IsNull(retrieved);        }    }}