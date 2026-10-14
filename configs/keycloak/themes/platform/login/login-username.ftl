
<#import "template.ftl" as layout>
<#import "field.ftl" as field>
<#import "buttons.ftl" as buttons>
<#import "social-providers.ftl" as identityProviders>
<#import "passkeys.ftl" as passkeys>

<@layout.registrationLayout
    displayMessage=!messagesPerField.existsError('username')
    displayInfo=(realm.password && realm.registrationAllowed && !registrationDisabled??);
    section>

    <#if section == "header">
        ${msg("loginAccountTitle")}

    <#elseif section == "form">
        <div id="kc-form">
            <div id="kc-form-wrapper">
                <#if realm.password>
                    <p class="debelo-login-subtitle">
                        ${msg("loginTitle", (realm.displayName!realm.name))}
                    </p>

                    <form id="kc-form-login"
                          class="${properties.kcFormClass!}"
                          action="${url.loginAction}"
                          method="post"
                          onsubmit="login.disabled = true; return true;">

                        <#if !usernameHidden??>
                            <#assign usernameLabel>
                                <#if !realm.loginWithEmailAllowed>
                                    ${msg("username")}
                                <#elseif !realm.registrationEmailAsUsername>
                                    ${msg("usernameOrEmail")}
                                <#else>
                                    ${msg("email")}
                                </#if>
                            </#assign>

                            <div class="${properties.kcFormGroupClass!}">
                                <@field.input
                                    name="username"
                                    label=usernameLabel
                                    value=login.username!''
                                    autofocus=true
                                    autocomplete="${(enableWebAuthnConditionalUI?has_content)?then('username webauthn', 'username')}" />
                            </div>
                        </#if>

                        <#if realm.rememberMe && !usernameHidden??>
                            <div class="debelo-form-options">
                                <@field.checkbox
                                    name="rememberMe"
                                    label=msg("rememberMe")
                                    value=login.rememberMe?? />
                            </div>
                        </#if>

                        <@buttons.loginButton />
                    </form>
                </#if>
            </div>
        </div>

        <@passkeys.conditionalUIData />

    <#elseif section == "info">
        <#if realm.password && realm.registrationAllowed && !registrationDisabled??>
            <div id="kc-registration">
                <span>${msg("noAccount")}</span>
                <a href="${url.registrationUrl}">${msg("doRegister")}</a>
            </div>
        </#if>

    <#elseif section == "socialProviders">
        <#if realm.password && social.providers?? && social.providers?has_content>
            <@identityProviders.show social=social />
        </#if>
    </#if>

</@layout.registrationLayout>
