<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title>Debelo Login</title>

    <#if properties.styles?has_content>
      <#list properties.styles?split('\\s+') as style>
        <link href="${url.resourcesPath}/${style}" rel="stylesheet"/>
      </#list>
    </#if>
</head>

<body class="custom-login-body">

<div class="page-container">

    <!-- BACKGROUND BRAND -->
    <div class="brand-background">

        <!-- woven fabric system -->
        <div class="woven-fabric">
            <div class="thread t1"></div>
            <div class="thread t2"></div>
            <div class="thread t3"></div>
            <div class="thread t4"></div>
        </div>

        <!-- soft glow -->
        <div class="fabric-glow"></div>

        <!-- brand content -->
        <div class="brand-content">
            <h1>Debelo</h1>
            <p>
                Threads of many ideas,<br/>
                woven into one fabric<br/>
                strong enough to carry everything that matters.
            </p>
        </div>

    </div>

    <!-- FLOATING LOGIN PANEL -->
    <div class="login-container">
        <div class="login-card">

            <div class="login-header">
                <h2>Welcome Back</h2>
                <p>Sign in to continue your work</p>
            </div>

            <#if message?has_content>
                <div class="alert">${message.summary}</div>
            </#if>

            <form id="kc-form-login" action="${url.loginAction}" method="post">
                <input type="text" name="username" placeholder="Email" required />
                <input type="password" name="password" placeholder="Password" required />

                <button type="submit">Continue</button>
            </form>

            <div class="login-footer">
                © 2026 Debelo
            </div>

        </div>
    </div>

</div>

</body>
</html>