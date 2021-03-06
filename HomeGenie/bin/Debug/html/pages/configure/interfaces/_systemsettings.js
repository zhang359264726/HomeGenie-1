HG.WebApp.SystemSettings = HG.WebApp.SystemSettings || {};

HG.WebApp.SystemSettings.InitializePage = function()
	{
		$('#page_configure_interfaces').on('pageinit', function (e) 
		{	        
			$('#systemsettings_zwaveoperation_popup').on('popupbeforeposition', function (event) {
				$('#systemsettings_zwaveoperation_close_button').addClass('ui-disabled');
				$('#systemsettings_zwaveoperation_nodeid').html('<span style="color:green">waiting</span>');
				$('#systemsettings_zwaveoperation_message').html('this operation will timeout in 10 seconds.');
		    });
		    //
			$('#page_configure_interfaces_zwaveport').change(function (event) {
			    HG.WebApp.SystemSettings.ZWaveSetPort($(this).val());
			});
		    //
			$('#page_configure_interfaces_x10port').change(function (event) {
			    HG.WebApp.SystemSettings.X10SetPort($(this).val());
			});
            //
	        $('#page_configure_interfaces_x10housecodes input[type=checkbox]').change(function(event) {
	    		var hc = '';
	        	//
	        	$('#page_configure_interfaces_x10housecodes input[type=checkbox]').each(function(){
	        		if ($(this).prop('checked'))
	        		{
		        		hc += $(this).val() + ',';
	        		}
	        	});
	        	//
	        	if (hc != '')
	        	{
	        		hc = hc.substr(0, hc.length - 1);
	        		HG.WebApp.SystemSettings.X10SetHouseCodes(hc);
	        		$('#control_groupslist').empty(); // forces control menu rebuild
	    		}
                else
                {
                    $(document).simpledialog2({
                        mode: 'blank',
                        headerText: 'Error',
                        headerClose: true,
                        blankContent : 
                          "<p style='margin:20px'>At least one house code must be selected.</p>"+
                          // NOTE: the use of rel="close" causes this button to close the dialog.
                          "<a rel='close' data-role='button' href='#'>Close</a>"
                    });
                }
	        });	
	        //
	        // Interfaces enable / disable switches
			//
			$( "#configure_interfaces_flip_zwave" ).on( 'slidestop', function( event ) {
	        	HG.WebApp.SystemSettings.RefreshOptions('zwave');
		        HG.Configure.Interfaces.ServiceCall("ZWave.SetIsEnabled/" + $( "#configure_interfaces_flip_zwave" ).val(), function (data) {
		            $('#control_groupslist').empty(); // forces control menu rebuild
		            $.mobile.hidePageLoadingMsg();
		        });
			});
			//
			$( "#configure_interfaces_flip_x10" ).on( 'slidestop', function( event ) {
	        	HG.WebApp.SystemSettings.RefreshOptions('x10');
		        HG.Configure.Interfaces.ServiceCall("X10.SetIsEnabled/" + $( "#configure_interfaces_flip_x10" ).val(), function (data) {
		            $('#control_groupslist').empty(); // forces control menu rebuild
		            $.mobile.hidePageLoadingMsg();
		        });
			});
			//
			$( "#configure_interfaces_flip_w800rf32" ).on( 'slidestop', function( event ) {
	        	HG.WebApp.SystemSettings.RefreshOptions('w800rf32');
		        HG.Configure.Interfaces.ServiceCall("W800RF.SetIsEnabled/" + $( "#configure_interfaces_flip_w800rf32" ).val(), function (data) {
		            $('#control_groupslist').empty(); // forces control menu rebuild
		            $.mobile.hidePageLoadingMsg();
		        });
			});
			//
			$( "#configure_interfaces_flip_raspigpio" ).on( 'slidestop', function( event ) {
		        HG.Configure.Interfaces.ServiceCall("RaspiGPIO.SetIsEnabled/" + $( "#configure_interfaces_flip_raspigpio" ).val(), function (data) {
		            $('#control_groupslist').empty(); // forces control menu rebuild
		            $.mobile.hidePageLoadingMsg();
		        });
			});
		    //
			$("#configure_interfaces_flip_weeco4mgpio").on('slidestop', function (event) {
			    HG.Configure.Interfaces.ServiceCall("Weeco4mGPIO.SetIsEnabled/" + $("#configure_interfaces_flip_weeco4mgpio").val(), function (data) {
			        $('#control_groupslist').empty(); // forces control menu rebuild
			        $.mobile.hidePageLoadingMsg();
			    });
			});
		    //
			$("#configure_interfaces_flip_upnp").on('slidestop', function (event) {
			    HG.Configure.Interfaces.ServiceCall("UPnP.SetIsEnabled/" + $("#configure_interfaces_flip_upnp").val(), function (data) {
			        $.mobile.hidePageLoadingMsg();
			    });
			});
		    //
			$( "#configure_interfaces_flip_lircremote" ).on( 'slidestop', function( event ) {
	        	HG.WebApp.SystemSettings.RefreshOptions('lircremote');
		        HG.Configure.Interfaces.ServiceCall("LircRemote.SetIsEnabled/" + $( "#configure_interfaces_flip_lircremote" ).val(), function (data) {
		            $.mobile.hidePageLoadingMsg();
		        });
			});
            //
			$( "#configure_interfaces_flip_camera" ).on( 'slidestop', function( event ) {
//	        	HG.WebApp.SystemSettings.RefreshOptions('camera');
		        HG.Configure.Interfaces.ServiceCall("CameraInput.SetIsEnabled/" + $( "#configure_interfaces_flip_camera" ).val(), function (data) {
		            $.mobile.hidePageLoadingMsg();
		        });
			});
            //
		    $("#configure_interfaces_lircremote_searchinput").keypress(function (e) {
                if (e.which != 13) return;
		        $.mobile.showPageLoadingMsg();
		        var $ul = $('#configure_interfaces_lircremote_search'),
					$input = $('#configure_interfaces_lircremote_searchinput'),
					value = $input.val(),
					html = "";
		        $ul.html("");
		        if (value && value.length > 2) {
		            $ul.html('<li><div class="ui-loader"><span class="ui-icon ui-icon-loading"></span></div></li>');
		            $ul.listview('refresh');
		            $.ajax({
		                url: '/' + HG.WebApp.Data.ServiceKey + '/Controllers.LircRemote/0/Remotes.Search/' + value + '/',
		                dataType: 'json'
		            })
					.then(function (response) {
                        response = eval(response);
					    $.each(response, function (i, val) {
					        html += '<li data-context-value="' + val.Manufacturer + '/' + val.Model + '" data-icon="plus"><a href="#">' + val.Manufacturer + ' ' + val.Model + '</a></li>';
					    });
					    $ul.html(html);
					    $ul.listview("refresh");
                        $ul.children('li').each(function () {
                            var remote = $(this).attr('data-context-value');
                            $(this).on('click', function(){
                                HG.WebApp.SystemSettings.LircRemoteAdd(remote);
                            });
                        });
					    $ul.trigger("create");
        		        $.mobile.hidePageLoadingMsg();
					});
		        }
		    });
		});   	
	};

HG.WebApp.SystemSettings.LircRemoteList = function ()
    {
		$.mobile.showPageLoadingMsg();
		$('#lirc_remotes').empty();
		$.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/Controllers.LircRemote/0/Remotes.List/'
        })
	    .then(function (response) {
            var remotes = eval(response);
            for(r = 0; r < remotes.length; r++)
            {
                var remdata = (remotes[r].Manufacturer + '/' + remotes[r].Model);
                $('#lirc_remotes').append($('<li/>', { 'data-icon' : 'minus', 'data-theme' : uitheme })
                                    .append($('<a/>', { 'href' : "javascript:HG.WebApp.SystemSettings.LircRemoteRemove('" + remdata + "')", 
                                                        'text' : remdata.replace('/', ' ') })));
            }
		    $('#lirc_remotes').listview('refresh');
            $.mobile.hidePageLoadingMsg();
	    });
    };

HG.WebApp.SystemSettings.LircRemoteAdd = function (remote)
    {
		$.mobile.showPageLoadingMsg();
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/Controllers.LircRemote/0/Remotes.Add/' + remote + '/'
        })
	    .then(function (response) {
            $.mobile.hidePageLoadingMsg();
            HG.WebApp.SystemSettings.LircRemoteList();
	    });
    };

HG.WebApp.SystemSettings.LircRemoteRemove = function (remote)
    {
		$.mobile.showPageLoadingMsg();
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/Controllers.LircRemote/0/Remotes.Remove/' + remote + '/'
        })
	    .then(function (response) {
            $.mobile.hidePageLoadingMsg();
            HG.WebApp.SystemSettings.LircRemoteList();
	    });
    };
	
HG.WebApp.SystemSettings.RefreshOptions = function (dname)
{
	if ($( "#configure_interfaces_flip_" + dname ).val() != "0")
	{
		$('[id=configure_interfaces_' + dname + 'options]').each(function(){ $(this).show() });
	}
	else	
	{
		$('[id=configure_interfaces_' + dname + 'options]').each(function(){ $(this).hide() });
	}
}

HG.WebApp.SystemSettings.LoadSettings = function () {
    $.mobile.showPageLoadingMsg();
    HG.Configure.Interfaces.ServiceCall("Hardware.SerialPorts", function (data) {
        var ports = eval(data);
        $('#page_configure_interfaces_zwaveport').empty();
        $('#page_configure_interfaces_zwaveport').append('<option value="">(select a port)</option>');
        if (ports.length == 0) {
            $('#page_configure_interfaces_zwaveport').append('<option value="">NO SERIAL PORTS FOUND</option>');
        }
        else {
            for (var p = 0; p < ports.length; p++) {
                $('#page_configure_interfaces_zwaveport').append('<option value="' + ports[p].replace(/\//g, '|') + '">' + ports[p] + '</option>');
            }
        }
        $('#page_configure_interfaces_zwaveport').selectmenu('refresh', true);
        //
        HG.Configure.Interfaces.ServiceCall("ZWave.GetPort", function (data) {
            $('#page_configure_interfaces_zwaveport').val(data).attr('selected', true).siblings('option').removeAttr('selected');
            $('#page_configure_interfaces_zwaveport').selectmenu('refresh', true);
            $.mobile.hidePageLoadingMsg();
            //
            $.mobile.showPageLoadingMsg();
            $('#page_configure_interfaces_x10port').empty().append('<option value="">(select a port)</option>');
            $('#page_configure_interfaces_x10port').append('<option value="USB">CM15 Pro - USB</option>');
            for (var p = 0; p < ports.length; p++) {
                $('#page_configure_interfaces_x10port').append('<option value="' + ports[p].replace(/\//g, '|') + '">CM11 - ' + ports[p] + '</option>');
            }
            $('#page_configure_interfaces_x10port').selectmenu('refresh', true);
            HG.Configure.Interfaces.ServiceCall("X10.GetPort", function (data) {
                $('#page_configure_interfaces_x10port').val(data).attr('selected', true).siblings('option').removeAttr('selected'); ;
                $('#page_configure_interfaces_x10port').selectmenu('refresh', true);
                $.mobile.hidePageLoadingMsg();
            });
            $('#page_configure_interfaces_w800rf32port').empty();
            for (var p = 0; p < ports.length; p++) {
                $('#page_configure_interfaces_w800rf32port').append('<option value="' + ports[p].replace(/\//g, '|') + '">' + ports[p] + '</option>');
            }
            HG.Configure.Interfaces.ServiceCall("W800RF.GetPort", function (data) {
                $('#page_configure_interfaces_w800rf32port').val(data).attr('selected', true).siblings('option').removeAttr('selected'); ;
                $('#page_configure_interfaces_w800rf32port').selectmenu('refresh', true);
                $.mobile.hidePageLoadingMsg();
            });
            HG.Configure.Interfaces.ServiceCall("ZWave.GetIsEnabled", function (data) {
                $('#configure_interfaces_flip_zwave').val(data).slider('refresh');
                HG.WebApp.SystemSettings.RefreshOptions('zwave');
                $.mobile.hidePageLoadingMsg();
            });
            HG.Configure.Interfaces.ServiceCall("X10.GetIsEnabled", function (data) {
                $('#configure_interfaces_flip_x10').val(data).slider('refresh');
                HG.WebApp.SystemSettings.RefreshOptions('x10');
                $.mobile.hidePageLoadingMsg();
            });
            HG.Configure.Interfaces.ServiceCall("W800RF.GetIsEnabled", function (data) {
                $('#configure_interfaces_flip_w800rf32').val(data).slider('refresh');
                HG.WebApp.SystemSettings.RefreshOptions('w800rf32');
                $.mobile.hidePageLoadingMsg();
            });
            HG.Configure.Interfaces.ServiceCall("LircRemote.GetIsEnabled", function (data) {
                $('#configure_interfaces_flip_lircremote').val(data).slider('refresh');
                HG.WebApp.SystemSettings.RefreshOptions('lircremote');
                $.mobile.hidePageLoadingMsg();
                HG.WebApp.SystemSettings.LircRemoteList();
            });
            HG.Configure.Interfaces.ServiceCall("CameraInput.GetIsEnabled", function (data) {
                $('#configure_interfaces_flip_camera').val(data).slider('refresh');
//                HG.WebApp.SystemSettings.RefreshOptions('camera');
                $.mobile.hidePageLoadingMsg();
            });
            HG.Configure.Interfaces.ServiceCall("RaspiGPIO.GetIsEnabled", function (data) {
                $('#configure_interfaces_flip_raspigpio').val(data).slider('refresh');
                $.mobile.hidePageLoadingMsg();
            });
            HG.Configure.Interfaces.ServiceCall("Weeco4mGPIO.GetIsEnabled", function (data) {
                $('#configure_interfaces_flip_weeco4mgpio').val(data).slider('refresh');
                $.mobile.hidePageLoadingMsg();
            });
            HG.Configure.Interfaces.ServiceCall("UPnP.GetIsEnabled", function (data) {
                $('#configure_interfaces_flip_upnp').val(data).slider('refresh');
                $.mobile.hidePageLoadingMsg();
            });
        });
    });
    HG.Configure.Interfaces.ServiceCall("X10.GetHouseCodes", function (data) {
        data = ',' + data + ',';
        $('#page_configure_interfaces_x10housecodes input[type=checkbox]').each(function () {
            if (data.indexOf(',' + $(this).val() + ',') >= 0) {
                $(this).prop('checked', true);
            }
            else {
                $(this).prop('checked', false);
            }
        });
        $('#page_configure_interfaces_x10housecodes input[type=checkbox]').checkboxradio('refresh');
        $.mobile.hidePageLoadingMsg();
    });
};
	
HG.WebApp.SystemSettings.ZWaveSetPort = function (port) 
	{
        $.mobile.showPageLoadingMsg();
        $.get('/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Interfaces.Configure/ZWave.SetPort/' + (port == '' ? '_' : port) + '/' + (new Date().getTime()), function (data) {
            $.mobile.hidePageLoadingMsg();
        });            
    };

HG.WebApp.SystemSettings.ZWaveNodeAdd = function (port) 
	{
	    $('#systemsettings_zwaveoperation_title').html('Add Node');
	    $('#systemsettings_zwaveoperation_popup').popup('open');
		zwave_NodeAdd(function(res) 
		{
			if (res != 0) 
			{
				HG.WebApp.Control._RefreshFn();
				//
				$('#systemsettings_zwaveoperation_nodeid').html(res);
				$('#systemsettings_zwaveoperation_message').html('node added.');
			}
			else	
			{
				$('#systemsettings_zwaveoperation_nodeid').html('<span style="color:red">timed out</span>');
				$('#systemsettings_zwaveoperation_message').html('operation falied.');
			}
		    $('#systemsettings_zwaveoperation_close_button').removeClass('ui-disabled');
		});
    };

HG.WebApp.SystemSettings.ZWaveNodeRemove = function (port) 
	{
	    $('#systemsettings_zwaveoperation_title').html('Remove Node');
	    $('#systemsettings_zwaveoperation_popup').popup('open');
		zwave_NodeRemove(function(res) 
		{
			if (res != 0) 
			{
				HG.WebApp.Control._RefreshFn();
				//
				$('#systemsettings_zwaveoperation_nodeid').html(res);
				$('#systemsettings_zwaveoperation_message').html('node removed.');
			}
			else	
			{
				$('#systemsettings_zwaveoperation_nodeid').html('<span style="color:red">timed out</span>');
				$('#systemsettings_zwaveoperation_message').html('operation falied.');
			}
		    $('#systemsettings_zwaveoperation_close_button').removeClass('ui-disabled');
		});
    };



HG.WebApp.SystemSettings.X10SetPort = function (port) {
	    $.mobile.showPageLoadingMsg();
	    $.get('/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Interfaces.Configure/X10.SetPort/' + port + '/' + (new Date().getTime()), function (data) {
	        $.mobile.hidePageLoadingMsg();
	    });            
	};

HG.WebApp.SystemSettings.X10SetHouseCodes = function (hcodes) {
        $.mobile.showPageLoadingMsg();
        $.get('/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Interfaces.Configure/X10.SetHouseCodes/' + hcodes + '/' + (new Date().getTime()), function (data) {
            HG.WebApp.Data.Modules = eval(arguments[2].responseText);
            $.mobile.hidePageLoadingMsg();
        });            
    };
    

HG.WebApp.SystemSettings.W800RfSetPort = function (port) {
	    $.mobile.showPageLoadingMsg();
	    $.get('/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Interfaces.Configure/W800RF.SetPort/' + port + '/' + (new Date().getTime()), function (data) {
	        $.mobile.hidePageLoadingMsg();
	    });            
	};


HG.WebApp.SystemSettings.CheckConfigureStatus = function () {

        var ifaceurl = '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Interfaces.List/' + (new Date().getTime());
        $.ajax({
            url: ifaceurl,
            dataType: 'json',
            success: function (data) {
                var interfaces = eval(data);
                if (!interfaces || interfaces == 'undefined' || interfaces.length == 0 || interfaces[0].Domain == 'HomeGenie.UpdateChecker')
                {
                    setTimeout(function () 
                    {
                        $('#homemenu_option_control').addClass('ui-disabled');
                        $('#popup_system_not_configured').popup().popup('open', { transition: 'slidedown' });
                    }, 2000);
                }
                else
                {
                    $('#homemenu_option_control').removeClass('ui-disabled');
                }
            }
        });   

    };