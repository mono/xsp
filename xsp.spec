Name:           xsp
Url:            http://go-mono.com/
License:        X11/MIT
Group:          Productivity/Networking/Web/Servers
AutoReqProv:    on
Version:        2.4.1
Release:        0
Summary:        Small Web Server Hosting ASP.NET
Source:         %{name}-%{version}.tar.bz2
BuildRoot:      %{_tmppath}/%{name}-%{version}-build
BuildArch:      noarch
BuildRequires:  mono-devel mono-web mono-data-oracle mono-jscript mono-nunit pkgconfig sqlite

#####  suse  ####
%if 0%{?suse_version}
PreReq:         %insserv_prereq %fillup_prereq
%define old_suse_buildrequires mono-data mono-winforms
%define _fwdefdir /etc/sysconfig/SuSEfirewall2.d/services

%if %suse_version == 1000
BuildRequires:  %{old_suse_buildrequires}
%endif

%if %sles_version == 9
BuildRequires:  %{old_suse_buildrequires}
%endif
%endif
# Fedora options (Bug in fedora images where 'abuild' user is the same id as 'nobody')
%if 0%{?fedora_version} || 0%{?rhel_version}
%define env_options export MONO_SHARED_DIR=/tmp
%endif

%define xspConfigsLocation %{_sysconfdir}/xsp/2.0
%define xspAvailableApps %{xspConfigsLocation}/applications-available
%define xspEnabledApps %{xspConfigsLocation}/applications-enabled

%description
The XSP server is a small Web server that hosts the Mono System.Web
classes for running what is commonly known as ASP.NET.

%prep
%setup -q

%build
%{?env_options}
# Cannot use the configure macro because noarch-redhat-linux is not recognized by the auto tools in the tarball
./configure --prefix=%{_prefix} \
	    --libexecdir=%{_prefix}/lib \
	    --libdir=%{_prefix}/lib \
	    --mandir=%{_mandir} \
	    --infodir=%{_infodir} \
	    --sysconfdir=%{_sysconfdir}
make

%install
%{?env_options}
make install DESTDIR=%{buildroot}
rm -rf %{buildroot}%{_prefix}/lib/xsp/unittests
mkdir -p %{buildroot}%{_datadir}
mv %{buildroot}%{_prefix}/lib/pkgconfig %{buildroot}%{_datadir}
%if 0%{?suse_version}
mkdir -p %{buildroot}/%{_fwdefdir}
mkdir -p %{buildroot}/%{xspAvailableApps}
mkdir -p %{buildroot}/%{xspEnabledApps}
mkdir -p %{buildroot}/etc/init.d/
mkdir -p %{buildroot}/etc/logrotate.d/
mkdir -p %{buildroot}/srv/xsp2
mkdir -p %{buildroot}/var/adm/fillup-templates
mkdir -p %{buildroot}/var/run/xsp2
install -m 644 man/mono-asp-apps.1 %{buildroot}%{_mandir}/man1/mono-asp-apps.1
install -m 644 packaging/opensuse/sysconfig.xsp2 %{buildroot}/var/adm/fillup-templates 
install -m 644 packaging/opensuse/xsp2.fw %{buildroot}/%{_fwdefdir}/xsp2
install -m 644 packaging/opensuse/xsp2.logrotate %{buildroot}/etc/logrotate.d/xsp2
install -m 755 packaging/opensuse/xsp2.init %{buildroot}/etc/init.d/xsp2
install -m 755 tools/mono-asp-apps/mono-asp-apps %{buildroot}%{_bindir}/mono-asp-apps
%endif

%clean
rm -rf %{buildroot}

%if 0%{?suse_version}
%post
%{fillup_and_insserv -n xsp2 xsp2}

%preun
%stop_on_removal xsp2

%postun
%restart_on_update xsp2
%{insserv_cleanup}

%endif

%files
%defattr(-,root,root)
%{_bindir}/*
%{_datadir}/pkgconfig/*
%{_prefix}/share/man/*/*
%{_prefix}/lib/xsp
%{_prefix}/lib/mono/gac/Mono.WebServer
%{_prefix}/lib/mono/1.0/Mono.WebServer.dll
%{_prefix}/lib/mono/gac/Mono.WebServer2
%{_prefix}/lib/mono/2.0/Mono.WebServer2.dll
%{_prefix}/lib/mono/gac/xsp
%{_prefix}/lib/mono/1.0/xsp.exe
%{_prefix}/lib/mono/gac/xsp2
%{_prefix}/lib/mono/2.0/xsp2.exe
%{_prefix}/lib/mono/gac/mod-mono-server
%{_prefix}/lib/mono/1.0/mod-mono-server.exe
%{_prefix}/lib/mono/gac/mod-mono-server2
%{_prefix}/lib/mono/2.0/mod-mono-server2.exe
%{_prefix}/lib/mono/gac/fastcgi-mono-server
%{_prefix}/lib/mono/1.0/fastcgi-mono-server.exe
%{_prefix}/lib/mono/gac/fastcgi-mono-server2
%{_prefix}/lib/mono/2.0/fastcgi-mono-server2.exe
%if 0%{?suse_version}
%config %{_fwdefdir}/xsp2
%config /etc/init.d/xsp2
%config /etc/logrotate.d/xsp2
/var/adm/fillup-templates/*
%attr(0711,wwwrun,www) /srv/xsp2
%attr(0711,wwwrun,www) /var/run/xsp2
%{_sysconfdir}/%{name}
%endif
%doc NEWS README

%if 0%{?fedora_version} || 0%{?rhel_version}
# Allows overrides of __find_provides in fedora distros... (already set to zero on newer suse distros)
%define _use_internal_dependency_generator 0
%endif
%define __find_provides env sh -c 'filelist=($(cat)) && { printf "%s\\n" "${filelist[@]}" | /usr/lib/rpm/find-provides && printf "%s\\n" "${filelist[@]}" | /usr/bin/mono-find-provides ; } | sort -u'
%define __find_requires env sh -c 'filelist=($(cat)) && { printf "%s\\n" "${filelist[@]}" | /usr/lib/rpm/find-requires && printf "%s\\n" "${filelist[@]}" | /usr/bin/mono-find-requires ; } | sort -u'

%changelog
