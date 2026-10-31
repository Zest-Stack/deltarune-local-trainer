// DELTARUNE Local Trainer bridge v1. No network, no automatic saving.
if (!variable_instance_exists(id, "cdt_poll"))
{
    cdt_poll = -1000;
    cdt_beat = -1;
    cdt_seen = -10000;
    cdt_ack = -1;
    cdt_enabled = 0;
    cdt_lockhp = 0;
    cdt_god = 0;
    cdt_locktp = 0;
    cdt_tp = 100;
    cdt_seq = -1;
    cdt_op = 0;
    cdt_value = 0;
    cdt_target = 0;
    cdt_initialized = false;
    global.cdt_active = false;
    global.cdt_god = false;
    global.cdt_lockhp = false;
}
var _cdt_pollnow = current_time - cdt_poll >= 200;
if (_cdt_pollnow)
{
    cdt_poll = current_time;
    if (file_exists("codex_trainer_control.ini"))
    {
        ini_open("codex_trainer_control.ini");
        var _cdt_beat = ini_read_real("control", "heartbeat", -1);
        if (_cdt_beat != cdt_beat)
        {
            cdt_beat = _cdt_beat;
            cdt_seen = current_time;
        }
        cdt_enabled = ini_read_real("control", "enabled", 0);
        cdt_lockhp = ini_read_real("control", "lockhp", 0);
        cdt_god = ini_read_real("control", "god", 0);
        cdt_locktp = ini_read_real("control", "locktp", 0);
        cdt_tp = clamp(ini_read_real("control", "tp", 100), 0, 100);
        cdt_seq = ini_read_real("control", "seq", -1);
        cdt_op = ini_read_real("control", "op", 0);
        cdt_value = ini_read_real("control", "value", 0);
        cdt_target = ini_read_real("control", "target", 0);
        ini_close();
        if (!cdt_initialized) { cdt_ack = cdt_seq; cdt_initialized = true; }
    }
    else cdt_enabled = 0;
}
global.cdt_active = cdt_enabled == 1 && current_time - cdt_seen < 3500;
global.cdt_god = global.cdt_active && cdt_god == 1;
global.cdt_lockhp = global.cdt_active && cdt_lockhp == 1;
var _cdt_ready = variable_global_exists("hp") && variable_global_exists("maxhp") && variable_global_exists("char") && (instance_exists(obj_mainchara) || instance_exists(obj_battlecontroller));
var _cdt_battle = instance_exists(obj_battlecontroller);
if (_cdt_ready && global.cdt_active)
{
    if (cdt_seq != cdt_ack && cdt_op != 0)
    {
        var _cdt_apply = cdt_op != 5 || _cdt_battle;
        if (_cdt_apply)
        {
            for (var _cdt_i = 0; _cdt_i < array_length(global.char); _cdt_i++)
            {
                var _cdt_c = global.char[_cdt_i];
                if (_cdt_c > 0 && _cdt_c < array_length(global.hp) && (cdt_target == 0 || cdt_target == _cdt_c))
                {
                    if (cdt_op == 1) global.hp[_cdt_c] = clamp(round(cdt_value), 1, global.maxhp[_cdt_c]);
                    if (cdt_op == 2)
                    {
                        global.maxhp[_cdt_c] = clamp(round(cdt_value), 1, 99999);
                        global.hp[_cdt_c] = global.maxhp[_cdt_c];
                    }
                    if (cdt_op == 3)
                    {
                        var _cdt_df = clamp(round(cdt_value), 0, 9999);
                        var _cdt_old = global.df[_cdt_c];
                        global.df[_cdt_c] = _cdt_df;
                        if (_cdt_battle && _cdt_i < 3) global.battledf[_cdt_i] += _cdt_df - _cdt_old;
                    }
                    if (cdt_op == 6) global.hp[_cdt_c] = global.maxhp[_cdt_c];
                }
            }
            if (cdt_op == 4) global.gold = clamp(round(cdt_value), 0, 9999999);
            if (cdt_op == 5) global.tension = global.maxtension * clamp(cdt_value, 0, 100) / 100;
            cdt_ack = cdt_seq;
        }
    }
    if (cdt_lockhp == 1)
    {
        for (var _cdt_i = 0; _cdt_i < array_length(global.char); _cdt_i++)
        {
            var _cdt_c = global.char[_cdt_i];
            if (_cdt_c > 0 && _cdt_c < array_length(global.hp)) global.hp[_cdt_c] = global.maxhp[_cdt_c];
        }
    }
    if (cdt_locktp == 1 && _cdt_battle) global.tension = global.maxtension * cdt_tp / 100;
}
if (_cdt_pollnow)
{
    var _cdt_f = file_text_open_write("codex_trainer_status_CHAPTER.txt");
    file_text_write_string(_cdt_f, "chapter=CHAPTER\nready=" + string(_cdt_ready) + "\nbattle=" + string(_cdt_battle) + "\nroom=" + room_get_name(room) + "\nack=" + string(cdt_ack) + "\nactive=" + string(global.cdt_active) + "\ngod=" + string(global.cdt_god) + "\nlockhp=" + string(cdt_lockhp) + "\nlocktp=" + string(cdt_locktp) + "\nsavepath=" + working_directory + "\n");
    if (_cdt_ready)
    {
        file_text_write_string(_cdt_f, "gold=" + string(global.gold) + "\ntp=" + string(global.tension / max(1,global.maxtension) * 100) + "\n");
        for (var _cdt_i = 0; _cdt_i < array_length(global.char); _cdt_i++)
        {
            var _cdt_c = global.char[_cdt_i];
            if (_cdt_c > 0 && _cdt_c < array_length(global.hp))
            {
                file_text_write_string(_cdt_f, "party" + string(_cdt_i) + "=" + string(_cdt_c) + "," + string(global.hp[_cdt_c]) + "," + string(global.maxhp[_cdt_c]) + "," + string(global.df[_cdt_c]) + "\n");
            }
        }
    }
    file_text_write_string(_cdt_f,"complete=1\n");
    file_text_close(_cdt_f);
}
