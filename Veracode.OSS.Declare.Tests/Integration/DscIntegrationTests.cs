﻿using Microsoft.Extensions.Configuration;
using Veracode.OSS.Declare.Shared;
using Veracode.OSS.Wrapper;

                var retrievedPolicy = _veracodeRepository.GetPolicies().Where(x => x.name == app.application_name).SingleOrDefault();

                foreach (var user in app.users)
                _dscLogic.MakeItSoPolicy(app, app.policy);
            {
                _dscLogic.MakeItSoScan(app, app.files.ToArray(), app.modules.ToArray());
                _dscLogic.MakeItSoMitigations(app);
            }
            TearDownFromConfiguration();

            _veracodeRepository.DeleteApp(new ApplicationType

            _veracodeRepository.DeletePolicy(retrievedPolicy.guid);