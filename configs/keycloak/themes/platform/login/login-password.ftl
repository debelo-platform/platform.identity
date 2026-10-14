
<#import "template.ftl" as layout>
<#import "field.ftl" as field>
<#import "buttons.ftl" as buttons>
<#import "passkeys.ftl" as passkeys>

<@layout.registrationLayout
    displayMessage=!messagesPerField.existsError('password');
    section>

    <#if section == "header">
        ${msg("doLogIn")}

    <#elseif section == "form">
        <div id="kc-form">
            <div id="kc-form-wrapper">
                <p class="debelo-login-subtitle">
                    ${msg("loginTitle", (realm.displayName!realm.name))}
                </p>

                <form id="kc-form-login"
                      class="${properties.kcFormClass!}"
                      action="${url.loginAction}"
                      method="post"
                      onsubmit="login.disabled = true; return true;">

                    <div class="debelo-password-field">
                        <@field.password
                            name="password"
                            label=msg("password")
                            forgotPassword=realm.resetPasswordAllowed
                            autofocus=true
                            autocomplete="current-password" />
                    </div>

                    <@buttons.loginButton />
                </form>
            </div>
        </div>

        <@passkeys.conditionalUIData />
    </#if>

</@layout.registrationLayout>
