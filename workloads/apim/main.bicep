var pfx = json(loadTextContent('../shared-env.json'))

@allowed([
  'dev'
  'tst'
  'uat'
  'prd'
])

param env string
param appSubscriptionId string 
param appServiceEnvironmentName string
param aud string
var region = 'aue'
var apiOperationsCollection = [  
  {
    name: 'func-get-employee-details'
    description: 'func-get-employee-details'
    displayName: 'func-get-employee-details'
    method: 'GET'
    urlTemplate: '/func-get-employee-details'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-get-escalate-form-task-by-current-date'
    description: 'func-get-escalate-form-task-by-current-date'
    displayName: 'func-get-escalate-form-task-by-current-date'
    method: 'GET'
    urlTemplate: '/func-get-escalate-form-task-by-current-date'
    templateParameters: []
    responses: []
  } 
  {
    name: 'func-get-form-appreference-by-form-info-id'
    description: 'func-get-form-appreference-by-form-info-id'
    displayName: 'func-get-form-appreference-by-form-info-id'
    method: 'GET'
    urlTemplate: '/func-get-form-appreference-by-form-info-id'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-get-form-appreference-by-user'
    description: 'func-get-form-appreference-by-user'
    displayName: 'func-get-form-appreference-by-user'
    method: 'GET'
    urlTemplate: '/func-get-form-appreference-by-user'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-get-form-email-by-form-info-id'
    description: 'func-get-form-email-by-form-info-id'
    displayName: 'func-get-form-email-by-form-info-id'
    method: 'GET'
    urlTemplate: '/func-get-form-email-by-form-info-id'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-get-form-email-by-user'
    description: 'func-get-form-email-by-user'
    displayName: 'func-get-form-email-by-user'
    method: 'GET'
    urlTemplate: '/func-get-form-email-by-user'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-get-form-history-by-form-info-id'
    description: 'func-get-form-history-by-form-info-id'
    displayName: 'func-get-form-history-by-form-info-id'
    method: 'GET'
    urlTemplate: '/func-get-form-history-by-form-info-id'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-get-form-info-by-id'
    description: 'func-get-form-info-by-id'
    displayName: 'func-get-form-info-by-id'
    method: 'GET'
    urlTemplate: '/func-get-form-info-by-id'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-get-form-task-by-form-info-id'
    description: 'func-get-form-task-by-form-info-id'
    displayName: 'func-get-form-task-by-form-info-id'
    method: 'GET'
    urlTemplate: '/func-get-form-task-by-form-info-id'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-get-form-task-by-user'
    description: 'func-get-form-task-by-user'
    displayName: 'func-get-form-task-by-user'
    method: 'GET'
    urlTemplate: '/func-get-form-task-by-user'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-get-ref-directorates-by-id'
    description: 'func-get-ref-directorates-by-id'
    displayName: 'func-get-ref-directorates-by-id'
    method: 'GET'
    urlTemplate: '/func-get-ref-directorates-by-id'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-get-ref-directorates-by-user'
    description: 'func-get-ref-directorates-by-user'
    displayName: 'func-get-ref-directorates-by-user'
    method: 'GET'
    urlTemplate: '/func-get-ref-directorates-by-user'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-get-ref-form-status-by-id'
    description: 'func-get-ref-form-status-by-id'
    displayName: 'func-get-ref-form-status-by-id'
    method: 'GET'
    urlTemplate: '/func-get-ref-form-status-by-id'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-get-ref-form-status-by-user'
    description: 'func-get-ref-form-status-by-user'
    displayName: 'func-get-ref-form-status-by-user'
    method:'GET'
    urlTemplate: '/func-get-ref-form-status-by-user'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-get-reminder-form-task-by-current-date'
    description: 'func-get-reminder-form-task-by-current-date'
    displayName: 'func-get-reminder-form-task-by-current-date'
    method: 'GET'
    urlTemplate: '/func-get-reminder-form-task-by-current-date'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-reusable-GetKeyVaultSecret-dev-001'
    description: 'func-reusable-GetKeyVaultSecret-dev-001'
    displayName: 'func-reusable-GetKeyVaultSecret-dev-001'
    method: 'GET'
    urlTemplate: '/func-reusable-GetKeyVaultSecret-dev-001'
    templateParameters: []
    responses: []
  }

  {
    name: 'func-create-update-form-appreference-details'
    description: 'func-create-update-form-appreference-details'
    displayName: 'func-create-update-form-appreference-details'
    method: 'POST'
    urlTemplate: '/func-create-update-form-appreference-details'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-create-update-form-details'
    description: 'func-create-update-form-details'
    displayName: 'func-create-update-form-details'
    method: 'POST'
    urlTemplate: '/func-create-update-form-details'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-create-update-form-email-details'
    description: 'func-create-update-form-email-details'
    displayName: 'func-create-update-form-email-details'
    method:'POST'
    urlTemplate: '/func-create-update-form-email-details'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-create-update-form-history-details'
    description:  'func-create-update-form-history-details'
    displayName: 'func-create-update-form-history-details'
    method: 'POST'
    urlTemplate: '/func-create-update-form-history-details'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-create-update-form-task-details'
    description: 'func-create-update-form-task-details'
    displayName: 'func-create-update-form-task-details'
    method: 'POST'
    urlTemplate: '/func-create-update-form-task-details'
    templateParameters: []
    responses: []
  }
  {
    name:  'func-create-update-ref-directorates'
    description:  'func-create-update-ref-directorates'
    displayName: 'func-create-update-ref-directorates'
    method: 'POST'
    urlTemplate: '/func-create-update-ref-directorates'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-create-update-ref-form-status'
    description: 'func-create-update-ref-form-status'
    displayName: 'func-create-update-ref-form-status'
    method: 'POST'
    urlTemplate: '/func-create-update-ref-form-status'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-create-e29-forms'
    description: 'func-create-e29-forms'
    displayName: 'func-create-e29-forms'
    method: 'POST'
    urlTemplate: '/func-create-e29-forms'
    templateParameters: []
    responses: []
  }
  {
    name: 'func-update-form-details'
    description: 'func-update-form-details'
    displayName: 'func-update-form-details'
    method: 'PUT'
    urlTemplate: '/func-update-form-details'
    templateParameters: []
    responses: []
  }
  {
    name: 'get-my-forms'
    description: 'get-my-forms'
    displayName: 'get-my-forms'
    method: 'GET'
    urlTemplate: '/get-my-forms'
    templateParameters: []
    responses: []
  }
  {
    name: 'get-all-directorate'
    description: 'get-all-directorate'
    displayName: 'get-all-directorate'
    method: 'GET'
    urlTemplate: '/get-all-directorate'
    templateParameters: []
    responses: []
  }
  {
    name: 'get-all-employee'
    description: 'get-all-employee'
    displayName: 'get-all-employee'
    method: 'GET'
    urlTemplate: '/get-all-employee'
    templateParameters: []
    responses: []
  }

  {
    name: 'upload-files'
    description: 'upload-files'
    displayName: 'upload-files'
    method: 'POST'
    urlTemplate: '/upload-files'
    templateParameters: []
    responses: []
  }
  {
    name: 'get-all-form-types'
    description: 'get-all-form-types'
    displayName: 'get-all-form-types'
    method: 'GET'
    urlTemplate: '/get-all-form-types'
    templateParameters: []
    responses: []
  }
  {
    name: 'get-all-form-statuses'
    description: 'get-all-form-statuses'
    displayName: 'get-all-form-statuses'
    method: 'GET'
    urlTemplate: '/get-all-form-statuses'
    templateParameters: []
    responses: []
  }
  {
    name: 'query-employees'
    description: 'query-employees'
    displayName: 'query-employees'
    method: 'GET'
    urlTemplate: '/query-employees'
    templateParameters: []
    responses: []
  }
  {
    name: 'query-user'
    description: 'query-user'
    displayName: 'query-user'
    method: 'GET'
    urlTemplate: '/query-user'
    templateParameters: []
    responses: []
  }
  {
    name: 'add-attachments'
    description: 'add-attachments'
    displayName: 'add-attachments'
    method: 'POST'
    urlTemplate: '/form-info/{formId}/add-attachments'
    templateParameters: [
      {
        name: 'formId'
        required: true
        type: 'number'
      }
    ]
    responses: []
  }
  {
    name: 'remove-attachment'
    description: 'remove-attachment'
    displayName: 'remove-attachment'
    method: 'POST'
    urlTemplate: '/form-info/{formId}/remove-attachment/{id}'
    templateParameters: [
      {
        name: 'formId'
        required: true
        type: 'number'
      }
      {
        name: 'id'
        required: true
        type: 'string'
      }
    ]
    responses: []
  }
  {
    name: 'view-attachment'
    description: 'view-attachment'
    displayName: 'view-attachment'
    method: 'GET'
    urlTemplate: '/form-info/{formId}/view-attachment/{id}'
    templateParameters: [
      {
        name: 'formId'
        required: true
        type: 'number'
      }
      {
        name: 'id'
        required: true
        type: 'string'
      }
    ]
    responses: []
  }
  {
    name: 'get-all-location'
    description: 'get-all-location'
    displayName: 'get-all-location'
    method: 'GET'
    urlTemplate: '/get-all-location'
    templateParameters: []
    responses: []
  }
  {
    name: 'get-location-rate'
    description: 'get-location-rate'
    displayName: 'get-location-rate'
    method: 'GET'
    urlTemplate: '/get-location-rate'
    templateParameters: []
    responses: []
  }
  {
    name: 'get-all-salary-rate'
    description: 'get-all-salary-rate'
    displayName: 'get-all-salary-rate'
    method: 'GET'
    urlTemplate: '/get-all-salary-rate'
    templateParameters: []
    responses: []
  }
  {
    name: 'get-all-region'
    description: 'get-all-region'
    displayName: 'get-all-region'
    method: 'GET'
    urlTemplate: '/get-all-region'
    templateParameters: []
    responses: []
  }
  {
    name: 'get-camp-rate'
    description: 'get-camp-rate'
    displayName: 'get-camp-rate'
    method: 'GET'
    urlTemplate: '/get-camp-rate'
    templateParameters: []
    responses: []
  }
  {
    name: 'get-all-time-travel'
    description: 'get-all-time-travel'
    displayName: 'get-all-time-travel'
    method: 'GET'
    urlTemplate: '/get-all-time-travel'
    templateParameters: []
    responses: []
  }
  {
    name: 'get-all-forms-by-parent'
    description: 'get-all-forms-by-parent'
    displayName: 'get-all-forms-by-parent'
    method: 'GET'
    urlTemplate: '/get-all-forms-by-parent'
    templateParameters: []
    responses: []
  }
  {
    name: 'get-all-forms-by-id'
    description: 'get-all-forms-by-id'
    displayName: 'get-all-forms-by-id'
    method: 'GET'
    urlTemplate: '/get-all-forms-by-id'
    templateParameters: []
    responses: []
  }
  {
    name: 'create-update-travel-proposal'
    description: 'create-update-travel-proposal'
    displayName: 'create-update-travel-proposal'
    method: 'POST'
    urlTemplate: '/create-update-travel-proposal'
    templateParameters: []
    responses: []
  }  
  {
    name: 'create-update-additional-hours-claims'
    description: 'create-update-additional-hours-claims'
    displayName: 'create-update-additional-hours-claims'
    method: 'POST'
    urlTemplate: '/create-update-additional-hours-claims'
    templateParameters: []
    responses: []
  }
  {
    name: 'create-update-casual-timesheets'
    description: 'create-update-casual-timesheets'
    displayName: 'create-update-casual-timesheets'
    method: 'POST'
    urlTemplate: '/create-update-casual-timesheets'
    templateParameters: []
    responses: []
  }  
  {
    name: 'create-update-motor-vehicle-allowance-claims'
    description: 'create-update-motor-vehicle-allowance-claims'
    displayName: 'create-update-motor-vehicle-allowance-claims'
    method: 'POST'
    urlTemplate: '/create-update-motor-vehicle-allowance-claims'
    templateParameters: []
    responses: []
  }  
  {
    name: 'create-update-other-allowance-claim'
    description: 'create-update-other-allowance-claim'
    displayName: 'create-update-other-allowance-claim'
    method: 'POST'
    urlTemplate: '/create-update-other-allowance-claim'
    templateParameters: []
    responses: []
  }  
  {
    name: 'create-update-out-of-hours-contact-claims'
    description: 'create-update-out-of-hours-contact-claims'
    displayName: 'create-update-out-of-hours-contact-claims'
    method: 'POST'
    urlTemplate: '/create-update-out-of-hours-contact-claims'
    templateParameters: []
    responses: []
  }  
  {
    name: 'create-update-overtime-claims'
    description: 'create-update-overtime-claims'
    displayName: 'create-update-overtime-claims'
    method: 'POST'
    urlTemplate: '/create-update-overtime-claims'
    templateParameters: []
    responses: []
  } 
  {
    name: 'create-update-penalty-shift-allowance-claims'
    description: 'create-update-penalty-shift-allowance-claims'
    displayName: 'create-update-penalty-shift-allowance-claims'
    method: 'POST'
    urlTemplate: '/create-update-penalty-shift-allowance-claims'
    templateParameters: []
    responses: []
  }   
  {
    name: 'create-update-sea-going-allowance-claim'
    description: 'create-update-sea-going-allowance-claim'
    displayName: 'create-update-sea-going-allowance-claim'
    method: 'POST'
    urlTemplate: '/create-update-sea-going-allowance-claim'
    templateParameters: []
    responses: []
  }      
  {
    name: 'get-form-btn'
    description: 'get-form-btn'
    displayName: 'get-form-btn'
    method: 'GET'
    urlTemplate: '/get-form-btn'
    templateParameters: []
    responses: []
  }
  {
    name: 'travel-proposal-approval'
    description: 'travel-proposal-approval'
    displayName: 'travel-proposal-approval'
    method: 'POST'
    urlTemplate: '/travel-proposal-approval'
    templateParameters: []
    responses: []
  } 
  {
    name: 'show-travel-cost'
    description: 'show-travel-cost'
    displayName: 'show-travel-cost'
    method: 'GET'
    urlTemplate: '/show-travel-cost'
    templateParameters: []
    responses: []
  } 
  {
    name: 'create-update-opr'
    description: 'create-update-opr'
    displayName: 'create-update-opr'
    method: 'POST'
    urlTemplate: '/create-update-opr'
    templateParameters: []
    responses: []
  } 
  {
    name: 'func-create-update-leave-request'
    description: 'func-create-update-leave-request'
    displayName: 'func-create-update-leave-request'
    method: 'POST'
    urlTemplate: '/func-create-update-leave-request'
    templateParameters: []
    responses: []
  }  
  {
    name: 'func-create-update-roster-change-request'
    description: 'func-create-update-roster-change-request'
    displayName: 'func-create-update-roster-change-request'
    method: 'POST'
    urlTemplate: '/func-create-update-roster-change-request'
    templateParameters: []
    responses: []
  }  
  {
    name: 'func-Create-Update-BusinessCaseNonAdv'
    description: 'func-Create-Update-BusinessCaseNonAdv'
    displayName: 'func-Create-Update-BusinessCaseNonAdv'
    method: 'POST'
    urlTemplate: '/func-Create-Update-BusinessCaseNonAdv'
    templateParameters: []
    responses: []
  } 
  {
    name: 'create-update-whsir'
    description: 'create-update-whsir'
    displayName: 'create-update-whsir'
    method: 'POST'
    urlTemplate: '/create-update-whsir'
    templateParameters: []
    responses: []
  } 
  {
    name: 'query-position'
    description: 'query-position'
    displayName: 'query-position'
    method: 'GET'
    urlTemplate: '/query-position'
    templateParameters: []
    responses: []
  }  
  {
    name: 'validate-pod-user'
    description: 'validate-pod-user'
    displayName: 'validate-pod-user'
    method: 'GET'
    urlTemplate: '/validate-pod-user'
    templateParameters: []
    responses: []
  }  
  {
    name: 'validate-odg-user'
    description: 'validate-odg-user'
    displayName: 'validate-odg-user'
    method: 'GET'
    urlTemplate: '/validate-odg-user'
    templateParameters: []
    responses: []
  }  
  {
    name: 'get-all-coa-funds'
    description: 'get-all-coa-funds'
    displayName: 'get-all-coa-funds'
    method: 'GET'
    urlTemplate: '/get-all-coa-funds'
    templateParameters: []
    responses: []
  }  
  {
    name: 'get-all-coa-cost-centres'
    description: 'get-all-coa-cost-centres'
    displayName: 'get-all-coa-cost-centres'
    method: 'GET'
    urlTemplate: '/get-all-coa-cost-centres'
    templateParameters: []
    responses: []
  }  
  {
    name: 'get-all-coa-locations'
    description: 'get-all-coa-locations'
    displayName: 'get-all-coa-locations'
    method: 'GET'
    urlTemplate: '/get-all-coa-locations'
    templateParameters: []
    responses: []
  }  
  {
    name: 'get-all-coa-activities'
    description: 'get-all-coa-activities'
    displayName: 'get-all-coa-activities'
    method: 'GET'
    urlTemplate: '/get-all-coa-activities'
    templateParameters: []
    responses: []
  }  
  {
    name: 'get-all-coa-projects'
    description: 'get-all-coa-projects'
    displayName: 'get-all-coa-projects'
    method: 'GET'
    urlTemplate: '/get-all-coa-projects'
    templateParameters: []
    responses: []
  }  
  {
    name: 'get-all-coa-account'
    description: 'get-all-coa-account'
    displayName: 'get-all-coa-account'
    method: 'GET'
    urlTemplate: '/get-all-coa-account'
    templateParameters: []
    responses: []
  } 
  {
  name: 'func-create-update-home-garaging-request'
  description: 'func-create-update-home-garaging-request'
  displayName: 'func-create-update-home-garaging-request'
  method: 'POST'
  urlTemplate: '/func-create-update-home-garaging-request'
  templateParameters: []
  responses: []
} 
  {
    name: 'get-all-ncLocation'
    description: 'get-all-ncLocation'
    displayName: 'get-all-ncLocation'
    method: 'GET'
    urlTemplate: '/get-all-ncLocation'
    templateParameters: []
    responses: []
  } 
  {
    name: 'get-all-wa-suburbs'
    description: 'get-all-wa-suburbs'
    displayName: 'get-all-wa-suburbs'
    method: 'GET'
    urlTemplate: '/get-all-wa-suburbs'
    templateParameters: []
    responses: []
  }
  {
  name: 'func-create-update-nonStandard-hardware-acquisition-request'
  description: 'func-create-update-nonStandard-hardware-acquisition-request'
  displayName: 'func-create-update-nonStandard-hardware-acquisition-request'
  method: 'POST'
  urlTemplate: '/func-create-update-nonStandard-hardware-acquisition-request'
  templateParameters: []
  responses: []
 }
]

var workloadSuffix = 'api'
var appResourceGroupName = '${pfx.clientPrefix}-az-${env}-${region}-rg-eforms' 
var functionAppKeyName = '${pfx.clientPrefix}-az-${env}-${region}-fun-eforms-key'
var functionAppName = '${pfx.clientPrefix}-az-${env}-${region}-fun-eforms'
var apimName = '${pfx.clientPrefix}-${region}-${env}-${pfx.apimCode}-${workloadSuffix}01'
var apiName = 'eform-API'
var functionAppId = resourceId(appSubscriptionId,appResourceGroupName,'Microsoft.Web/sites',functionAppName)

resource namedValue 'Microsoft.ApiManagement/service/namedValues@2021-08-01' = {
  name: '${apimName}/${functionAppKeyName}'
  dependsOn: [
  ]
  properties: {
    displayName: functionAppKeyName
    secret: true
    tags: [
      'key'
      'function'
    ]
    value: listkeys('${functionAppId}/host/default','2016-08-01').functionKeys.default
  }
}
output namedValueStatus string = 'namedValue: ${functionAppKeyName} has been created'

resource apiServicePolicy 'Microsoft.ApiManagement/service/policies@2021-08-01' = {
  name: '${apimName}/policy'
  properties: {
    format: 'xml'
    value: '<policies>\r\n    <inbound>\r\n    <cors allow-credentials="false">\r\n    <allowed-origins>\r\n    <origin>*</origin>\r\n    </allowed-origins>\r\n    <allowed-methods>\r\n    <method>GET</method>\r\n   <method>PUT</method>\r\n    <method>POST</method>\r\n    <method>OPTIONS</method>\r\n    </allowed-methods>\r\n    <allowed-headers>\r\n    <header>*</header>\r\n    </allowed-headers>\r\n    </cors>\r\n    </inbound>\r\n    <backend>\r\n    <forward-request />\r\n    </backend>\r\n    <outbound />\r\n    <on-error />\r\n    </policies>'
  }
}

resource eformApi 'Microsoft.ApiManagement/service/apis@2021-08-01' = {
  name: '${apimName}/${apiName}'
  dependsOn: [
    backend
  ]
  properties: {
    apiRevision: '1'
    displayName: 'eForm API'
    isCurrent: true
    path: '/'
    protocols: [
      'https'
    ]
    subscriptionRequired: false
  }
}

resource backend 'Microsoft.ApiManagement/service/backends@2021-08-01' = {
  name: '${apimName}/eFormBackend'
  dependsOn: [
    namedValue
  ]
  properties: {
    credentials: {
      header: {
        'x-functions-key': [
          '{{${functionAppKeyName}}}'
        ]
      }
    }
    description: 'Backend for eform API'
    protocol: 'http'
    resourceId: 'https://management.azure.com/subscriptions/${appSubscriptionId}/resourceGroups/${appResourceGroupName}/providers/Microsoft.Web/sites/${functionAppName}'
    url: 'https://${functionAppName}.${appServiceEnvironmentName}.appserviceenvironment.net/api'
  }
}

var policyStart = '''<policies>
    <inbound>
        <base />
        <set-backend-service id="apim-generated-policy" backend-id="eFormBackend" />
        <validate-jwt header-name="Authorization" failed-validation-httpcode="401" failed-validation-error-message="Unauthorized. Access token is missing or invalid.">
            <openid-config url="https://login.microsoftonline.com/ced71ed6-76dd-43d0-9acc-cf122b3bc423/v2.0/.well-known/openid-configuration" />
            <required-claims>
                <claim name="aud">
                    <value>'''
var policyEnd = '''</value>
                </claim>
            </required-claims>
        </validate-jwt>
        <set-header name="upn" exists-action="override">
            <value>@(context.Request.Headers["Authorization"].First().Split(' ')[1].AsJwt()?.Claims["preferred_username"].FirstOrDefault())</value>
        </set-header>
        <set-header name="Ocp-Apim-Subscription-Key" exists-action="delete" />
        <set-header name="Authorization" exists-action="delete" />
    </inbound>
    <backend>
        <base />
    </backend>
    <outbound>
        <base />
    </outbound>
    <on-error>
        <base />
    </on-error>
</policies>'''

resource eformApiPolicy 'Microsoft.ApiManagement/service/apis/policies@2021-08-01' = {
  name: '${apimName}/${apiName}/policy'
  dependsOn: [
    eformApi
    backend
  ]
  properties: {
    format: 'xml'
    value: '${policyStart}${aud}${policyEnd}'
  }
}

resource apiOperations 'Microsoft.ApiManagement/service/apis/operations@2021-08-01' = [for item in apiOperationsCollection: {
  name: '${apimName}/${apiName}/${item.name}'
  dependsOn: [
    eformApi
  ]
  properties: {
    description: item.description
    displayName: item.displayName
    method: item.method
    responses: [
    ]
    templateParameters: item.templateParameters
    urlTemplate: item.urlTemplate
  }
}]


