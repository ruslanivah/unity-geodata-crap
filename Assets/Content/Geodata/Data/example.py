node = hou.pwd()
geo = node.geometry()

'''
 Written by Atom (c) 2015.
 Data Comes From: http://imbrium.mit.edu/DATA/LOLA_GDR/
 http://lunar.gsfc.nasa.gov/lola/
 https://en.wikipedia.org/wiki/Clementine_%28spacecraft%29#Mission
 Clementine data show a range of about 18,100 meters from lowest to highest point on the Moon.
 The highest point, located on the far side of the Moon, is approximately 6500 meters higher than Mons Huygens (usually listed as the tallest mountain @4.7Km).
'''

import os, math, array,struct

def returnValueForToken(passedLines, passedToken):
    result = None
    for line in passedLines:
        if line.find(passedToken) != -1:      # Look for token.
            n = line.find("=")                # Look for = symbol.
            if n !=-1:
                temp = line[n+1:]
                n = temp.find("<")            # Look for trailing junk to remove.
                if n != -1:
                    result = float(temp[:n-1])
                    break
                else:
                    result = float(temp)
                    break
    return result

# A range with a float step.
def xfrange(start, stop, step):
    while start < stop:
        yield start
        start += step

# Get filename from user controls and construct companion label file name.
filename = node.evalParm('ctl_filename')
name_only = os.path.splitext(filename)[0]
label_filename = "%s%s" % (name_only,".lbl")

#print filename
#print label_filename

# Fetch related information from the .LBL file.
with open(label_filename, 'r') as infile:
    data = infile.read()        # Read the contents of the file into memory.
lst_lines = data.splitlines()   # Return a list of the lines, breaking at line boundaries.

# Grab token data from the list of lines.
MAP_RESOLUTION = returnValueForToken(lst_lines,"MAP_RESOLUTION")
ROW_COUNT = returnValueForToken(lst_lines,"LINE_LAST_PIXEL")
COLUMN_COUNT = returnValueForToken(lst_lines,"SAMPLE_LAST_PIXEL")
MIN_LAT = returnValueForToken(lst_lines,"MINIMUM_LATITUDE")
MAX_LAT = returnValueForToken(lst_lines,"MAXIMUM_LATITUDE")
MIN_LONG = returnValueForToken(lst_lines,"WESTERNMOST_LONGITUDE")
MAX_LONG = returnValueForToken(lst_lines,"EASTERNMOST_LONGITUDE")
DATA_SCALE = returnValueForToken(lst_lines,"SCALING_FACTOR")
MOON_RADIUS = returnValueForToken(lst_lines,"OFFSET")*0.01
SAMPLE_BITS = returnValueForToken(lst_lines,"SAMPLE_BITS")
if SAMPLE_BITS == 16:
    DATA_SIZE = 2
    DATA_TYPE = "h"
    # Integer .LBL files do not have MIN/MAX tokens but we'll go ahead and guess.
    MIN_HEIGHT = -8.746     #-9.127
    MAX_HEIGHT = 10.380     #10.777
    TRUE_MIN = -100
    TRUE_MAX = 100
else:
    DATA_SIZE = 4                       # Number of bytes to represent a number in the .IMG file.
    DATA_TYPE = "d"                     # or "h" for original integer style data.
    # Taken from the float based .LBL file for this .IMG.
    MIN_HEIGHT = returnValueForToken(lst_lines,"MINIMUM")*0.1   #*1000  # in kilometers       #-8.746     #-9.127
    MAX_HEIGHT = returnValueForToken(lst_lines,"MAXIMUM")*0.1   #*1000  # in kilometers       #10.380     #10.777
    TRUE_MIN = -9.15
    TRUE_MAX = 10.76


# Fetch values from user controls.
GRID_RES= float(1.0/MAP_RESOLUTION)   #float(1.0/MAP_RESOLUTION)
XINC = COLUMN_COUNT/MAP_RESOLUTION * `ch("scale_grid")`
YINC = ROW_COUNT/MAP_RESOLUTION * `ch("scale_grid")`

#print MAP_RESOLUTION,ROW_COUNT,COLUMN_COUNT,MIN_LAT,MAX_LAT,MIN_LONG,MAX_LONG,DATA_SCALE,MOON_RADIUS,SAMPLE_BITS,MIN_HEIGHT,MAX_HEIGHT,GRID_RES
#print(MAP_RESOLUTION)
POINTS=COLUMN_COUNT
LINES=ROW_COUNT
SCALING_FACTOR=DATA_SCALE   #`ch("scale_height")`

#Data inside the map file. Check if you use different map
START_LONG=0    #0.0078125                # Westernmost longitude
START_LAT=90    #89.9921875                # Max latitude
#MOON_RADIUS=1737.400                # Radius of the moon
TO_RAD=math.pi/180                  # From degrees to radians

#python fit.
def remap( x, oMin, oMax, nMin, nMax ):
    #range check
    if oMin == oMax:
        print "Warning: Zero input range"
        return None

    if nMin == nMax:
        print "Warning: Zero output range"
        return None

    #check reversed input range
    reverseInput = False
    oldMin = min( oMin, oMax )
    oldMax = max( oMin, oMax )
    if not oldMin == oMin:
        reverseInput = True

    #check reversed output range
    reverseOutput = False
    newMin = min( nMin, nMax )
    newMax = max( nMin, nMax )
    if not newMin == nMin :
        reverseOutput = True

    portion = (x-oldMin)*(newMax-newMin)/(oldMax-oldMin)
    if reverseInput:
        portion = (oldMax-x)*(newMax-newMin)/(oldMax-oldMin)

    result = portion + newMin
    if reverseOutput:
        result = newMax - portion

    return result

def returnColumnForLongitude(passedLongitudeMin, passedLongitudeMax, passedResolution, passedLongitude):
    if passedLongitude < passedLongitudeMin or passedLongitude > passedLongitudeMax:
        # Longitude is out of bounds for this data set.
        return None
    else:
        index = 0
        start = passedLongitudeMin
        while (start <= passedLongitude):
            start += passedResolution
            index +=1
        return index

def returnRowForLatitude(passedLatitudeMin, passedLatitudeMax, passedResolution, passedLatitude):
    if passedLatitude < passedLatitudeMin or passedLatitude > passedLatitudeMax:
        # Latitude is out of bounds for this data set.
        return None
    else:
        index = 0
        start = passedLatitudeMin
        while (start <= passedLatitude):
            start += passedResolution
            index +=1
        return index

def returnSampleRowData(filename, row_index, passedColumnMax, passedDataSize, passedDataType):
    max_amount = 32768
    seek_amount = int(passedColumnMax * passedDataSize * row_index)
    # Scan the data set for the requested row of samples.
    f=open(filename,'rb')                                   # Open the file
    if seek_amount > max_amount:
        #Beyond standard seek range, seek in max amount steps.
        seek_now = seek_amount
        while (seek_now > max_amount):
            f.seek(int(max_amount),1)                       # Seek points relative to the last seek.
            seek_now -= max_amount
        f.seek(int(seek_now),1)                             # Seek the final amount.
    else:
        f.seek(seek_amount,1)
    read_amount = int(passedColumnMax*passedDataSize)
    if passedDataSize == 4:
        dataHeight = array.array('f')
        dataHeight.fromfile(f, int(passedColumnMax))
    else:
        dataHeight = f.read(read_amount)
        dataHeight = array.array(passedDataType,dataHeight)
    f.close()
    return dataHeight

#Input: Longitude
#Output: Number of the point on row (or column)
def returnPoint(Long):
    tmp_point=round((Long-START_LONG)/GRID_RES+1)
    if tmp_point>POINTS:tmp_point=POINTS
    return tmp_point

#Input: Latitude
#Output: Number of the line (or row)
def returnLineIndex(passedLatitude):
    tmp_lat = MAX_LAT-passedLatitude
    tmp = ROW_COUNT/tmp_lat

def returnLine(Lat):
    tmp_line=round((START_LAT-Lat)/GRID_RES+1)
    if tmp_line>LINES:tmp_line=LINES
    return tmp_line

#Input: Line number (or row)
#Output: Latitude
def Lat(Line):
    return START_LAT-(Line-1)*GRID_RES

#Input: Point (or column)
#Output: Longitude
def Long(Point):
    return START_LONG+(Point-1)*GRID_RES

def rowsToPoints(CREATE_POINT = False):
    lst_vertices = []
    lst_colors = []

    cs = `ch("column_start")`
    ce = cs + `ch("column_range")`
    rs = `ch("row_start")`
    re = rs+`ch("row_range")`
    y_pos = 1
    for row_index in xfrange(rs, re, 1):
        x_pos = 1
        lst_samples = returnSampleRowData(filename, row_index, COLUMN_COUNT, DATA_SIZE, DATA_TYPE)
        for column_index in xfrange(cs, ce,1):
            color_min = False
            color_max = False
            data_height = lst_samples[column_index]
            # Bound data by min/max values specified in the companion .LBL file for this .IMG.
            if data_height < MIN_HEIGHT:
                #data_height = MIN_HEIGHT
                if `ch("tgl_colorize")`:color_min = True
            if data_height > MAX_HEIGHT:
                #data_height = MAX_HEIGHT
                if `ch("tgl_colorize")`:color_max = True

            X = x_pos*`ch("scale_grid")`
            Z = y_pos*`ch("scale_grid")`
            Y = data_height*`ch("scale_height")`
            if CREATE_POINT:
                # Create the point
                pt0 = geo.createPoint()
                pt0.setPosition(hou.Vector3(X,Y,Z))

            # Store values for result.
            lst_vertices.append((float(X),float(Y),float(Z)))
            lst_colors.append((color_min,color_max))
            x_pos+=1
        y_pos+=1
    return lst_vertices, lst_colors, (re-rs), (ce-cs)

def sectionToPoints(lat_from, lat_to,long_from, long_to, CREATE_POINT = False):
    lst_vertices = []
    lst_colors = []

    # Determine direction of travel for LAT/LONG.
    if (lat_from>lat_to):
        lat_dir = -GRID_RES
    else:
        lat_dir = GRID_RES
    if (long_from>long_to):
        long_dir = -GRID_RES
    else:
        long_dir = GRID_RES

    row_count = 0
    last_row_index = -1
    for now_lat in xfrange(lat_from, lat_to, lat_dir):
        row_index = returnRowForLatitude(MIN_LAT, MAX_LAT, GRID_RES, now_lat)
        if row_index != None:
            if now_lat == lat_from: print "Starting on line #%s" % row_index
            # A valid row, fetch full column data for this row.
            if row_index != last_row_index:
                column_count = 0
                lst_samples = returnSampleRowData(filename, row_index, COLUMN_COUNT, DATA_SIZE, DATA_TYPE)
                for now_long in xfrange(long_from, long_to, long_dir):
                    color_min = False
                    color_max = False
                    column_index = returnColumnForLongitude(MIN_LONG, MAX_LONG, GRID_RES, now_long)
                    if column_index != None:
                        color_min = False
                        color_max = False
                        # Fetch data height value for this row and column.
                        try:
                            data_height = lst_samples[column_index]
                        except:
                            data_height = MIN_HEIGHT-1
                        # Bound data by min/max values specified in the companion .LBL file for this .IMG.
                        if data_height < MIN_HEIGHT:
                            if `ch("tgl_bound_height")`:data_height = MIN_HEIGHT
                            if `ch("tgl_colorize")`:color_min = True
                        if data_height > MAX_HEIGHT:
                            if `ch("tgl_bound_height")`:data_height = MAX_HEIGHT
                            if `ch("tgl_colorize")`:color_max = True
                        if node.evalParm('tgl_aesthetic'):
                            # Aesthetic rescale of data, not scientific.
                            dh = remap(data_height, TRUE_MIN, TRUE_MAX, MIN_HEIGHT, MAX_HEIGHT)
                        else:
                            # Unaltered height data.
                            dh = data_height
                        radius = (dh*`ch("scale_height")`)+MOON_RADIUS
                        radius_current = radius*(math.cos(now_lat*TO_RAD))
                        X=radius_current*(math.sin(now_long*TO_RAD))
                        Y=radius*math.sin(now_lat*TO_RAD)
                        Z=radius_current*math.cos(now_long*TO_RAD)

                        if CREATE_POINT:
                            # Create the point
                            pt0 = geo.createPoint()
                            pt0.setPosition(hou.Vector3(X,Y,Z))
                            if color_min:
                                pt0.setAttribValue(attrib_Cd, (0.0,0.0,0.5))
                            if color_max:
                                pt0.setAttribValue(attrib_Cd, (0.5,0.0,0.0))
                            # Store data_height, now_lat and now long with this point.
                            pt0.setAttribValue(height_attrib, dh)
                            pt0.setAttribValue(now_lat_attrib, float(now_lat))
                            pt0.setAttribValue(now_long_attrib, float(now_long))
                    else:
                        print "unable to obtain longitude column index @ %s degrees for range %s,%s" % (now_long, MIN_LONG,MAX_LONG)
                    # Store values for result.
                    lst_vertices.append((float(X),float(Y),float(Z)))
                    lst_colors.append((color_min,color_max))
                    column_count += 1
                row_count += 1
            else:
                print "skipping latitude %s because row_index is the same as last." % now_lat
        else:
            print "%s latitude is out of range for range %s,%s" % (now_lat, MIN_LAT,MAX_LAT)

        last_row_index = row_index
    return lst_vertices, lst_colors, row_count, column_count

def mapPointsToFaces (passedRowCount, passedColumnCount):
    points = geo.points()
    if len(points):
        for row_index in range(passedRowCount-1):
            for column_index in range(passedColumnCount-1):
                vertex_index = column_index+(row_index * passedColumnCount)
                p1 = points[vertex_index]
                p2 = points[vertex_index+1]
                vertex_index = column_index+((row_index+1) * passedColumnCount)
                p3 = points[vertex_index]
                p4 = points[vertex_index+1]
                poly = geo.createPolygon()
                # Polygon vertex order is clock-wise.
                poly.addVertex(p1);
                poly.addVertex(p2);
                poly.addVertex(p4);
                poly.addVertex(p3);

def vertsToFaces (passedVertices, passedColorFlags, passedRowCount, passedColumnCount):
    len_row = passedRowCount-1
    len_column = passedColumnCount-1
    for row_index in range(passedRowCount-1):
        for column_index in range(passedColumnCount-1):
            # Default to white.
            v1_color = (1.0,1.0,1.0)
            v2_color = (1.0,1.0,1.0)
            v3_color = (1.0,1.0,1.0)
            v4_color = (1.0,1.0,1.0)
            vertex_index = column_index+(row_index * passedColumnCount)
            try:
                v1 = passedVertices[vertex_index]
                if passedColorFlags[vertex_index][0]:v1_color = (0.0,0.0,0.5)     # colorize minimum bounds.
                if passedColorFlags[vertex_index][1]:v1_color = (0.5,0.0,0.0)     # colorize maximum bounds.
            except:
                v1 = passedVertices[0]
            try:
                v2 = passedVertices[vertex_index+1]
                if passedColorFlags[vertex_index+1][0]:v2_color = (0.0,0.0,0.5)     # colorize minimum bounds.
                if passedColorFlags[vertex_index+1][1]:v2_color = (0.5,0.0,0.0)     # colorize maximum bounds.
            except:
                v2 = passedVertices[0]

            vertex_index = column_index+((row_index+1) * passedColumnCount)
            try:
                v3 = passedVertices[vertex_index]
                if passedColorFlags[vertex_index][0]:v3_color = (0.0,0.0,0.5)     # colorize minimum bounds.
                if passedColorFlags[vertex_index][1]:v3_color = (0.5,0.0,0.0)     # colorize maximum bounds.
            except:
                v3 = passedVertices[0]
            try:
                v4 = passedVertices[vertex_index+1]
                if passedColorFlags[vertex_index+1][0]:v4_color = (0.0,0.0,0.5)     # colorize minimum bounds.
                if passedColorFlags[vertex_index+1][1]:v4_color = (0.5,0.0,0.0)     # colorize maximum bounds.
            except:
                v4 = passedVertices[0]

            pt0 = geo.createPoint()
            pt0.setPosition(hou.Vector3(v1[0], v1[1], v1[2]))
            pt1 = geo.createPoint()
            pt1.setPosition(hou.Vector3(v2[0], v2[1], v2[2]))
            pt2 = geo.createPoint()
            pt2.setPosition(hou.Vector3(v3[0], v3[1], v3[2]))
            pt3 = geo.createPoint()
            pt3.setPosition(hou.Vector3(v4[0], v4[1], v4[2]))
            if `ch("tgl_colorize")`:
                pt0.setAttribValue(attrib_Cd, v1_color)
                pt1.setAttribValue(attrib_Cd, v2_color)
                pt2.setAttribValue(attrib_Cd, v3_color)
                pt3.setAttribValue(attrib_Cd, v4_color)
            poly = geo.createPolygon()
            # Polygon vertex order is clock-wise.
            poly.addVertex(pt0);
            poly.addVertex(pt1);
            poly.addVertex(pt3);
            poly.addVertex(pt2);
            #break
        #break

def areaToPoints(filename, lat_from, lat_to,long_from, long_to, CREATE_POINT = False):
    # Create a set of Houdini points, at world origin, that matches the latitude/longitude range.
    # Store the data height, curent latitude and logitude with each point.

    # Determine direction of travel for LAT/LONG.
    if (lat_from>lat_to):
        lat_dir = -GRID_RES
    else:
        lat_dir = GRID_RES
    if (long_from>long_to):
        long_dir = -GRID_RES
    else:
        long_dir = GRID_RES

    row_count = 0
    last_row_index = -1
    for now_lat in xfrange(lat_from, lat_to, lat_dir):
        row_index = returnRowForLatitude(MIN_LAT, MAX_LAT, GRID_RES, now_lat)
        if row_index != None:
            if now_lat == lat_from: print "Starting on line #%s" % row_index
            # A valid row, fetch full column data for this row.
            if row_index != last_row_index:
                column_count = 0
                lst_samples = returnSampleRowData(filename, row_index, COLUMN_COUNT, DATA_SIZE, DATA_TYPE)
                for now_long in xfrange(long_from, long_to, long_dir):
                    color_min = False
                    color_max = False
                    column_index = returnColumnForLongitude(MIN_LONG, MAX_LONG, GRID_RES, now_long)
                    if column_index != None:
                        color_min = False
                        color_max = False
                        # Fetch data height value for this row and column.
                        try:
                            data_height = lst_samples[column_index]
                        except:
                            data_height = MIN_HEIGHT-1
                        # Bound data by min/max values specified in the companion .LBL file for this .IMG.
                        if data_height < MIN_HEIGHT:
                            if `ch("tgl_bound_height")`:data_height = MIN_HEIGHT
                            if `ch("tgl_colorize")`:color_min = True
                        if data_height > MAX_HEIGHT:
                            if `ch("tgl_bound_height")`:data_height = MAX_HEIGHT
                            if `ch("tgl_colorize")`:color_max = True
                        #dh = remap(data_height, TRUE_MIN, TRUE_MAX, MIN_HEIGHT, MAX_HEIGHT)
                        # Don't calculate postion yet, let VEX do that for faster execution.
                        X = 0
                        Y = 0
                        Z = 0

                        if CREATE_POINT:
                            # Create the point
                            pt0 = geo.createPoint()
                            pt0.setPosition(hou.Vector3(X,Y,Z))
                            if color_min:
                                pt0.setAttribValue(attrib_Cd, (0.0,0.0,0.5))
                            if color_max:
                                pt0.setAttribValue(attrib_Cd, (0.5,0.0,0.0))
                            # Store data_height, now_lat and now long with this point.
                            pt0.setAttribValue(height_attrib, data_height)
                            pt0.setAttribValue(now_lat_attrib, float(now_lat))
                            pt0.setAttribValue(now_long_attrib, float(now_long))
                    else:
                        print "unable to obtain longitude column index @ %s degrees for range %s,%s" % (now_long, MIN_LONG,MAX_LONG)
                    # Store values for result.
                    #lst_vertices.append((float(X),float(Y),float(Z)))
                    #lst_colors.append((color_min,color_max))
                    column_count += 1
                row_count += 1
            else:
                print "skipping latitude %s because row_index is the same as last." % now_lat
        else:
            print "%s latitude is out of range for range %s,%s" % (now_lat, MIN_LAT,MAX_LAT)

        last_row_index = row_index
    column_count_attrib = geo.addAttrib(hou.attribType.Global, "column_count", column_count)
    row_count_attrib = geo.addAttrib(hou.attribType.Global, "row_count", row_count)
    #map_res_attrib = geo.addAttrib(hou.attribType.Global, "map_res", MAP_RESOLUTION)
    return #lst_vertices, lst_colors, row_count, column_count

# User controls.
ch_lat_from = `ch("ctl_ALat")` + `ch("ctl_OffLat")`
ch_lat_to = `ch("ctl_DaLat")`+ `ch("ctl_OffLat")`
ch_long_from = `ch("ctl_DaLong")`+ `ch("ctl_OffLong")`
ch_long_to = `ch("ctl_ALong")`+ `ch("ctl_OffLong")`

# Generate Moon Surface from LDEM data.
#print "clear"
geo.clear()
attrib_Cd = geo.addAttrib(hou.attribType.Point, "Cd", (1.0,1.0,1.0))
height_attrib = geo.addAttrib(hou.attribType.Point, "dem_height", 0.0)
now_lat_attrib = geo.addAttrib(hou.attribType.Point, "now_latitude", 0.0)
now_long_attrib = geo.addAttrib(hou.attribType.Point, "now_longitude", 0.0)

projection_type = 0
if projection_type == 0:
    # Spherical Projection.
    if `ch("tgl_make_faces")` == True:
        # Sperical projection.
        lst_vertices, lst_colors, row_count, column_count = sectionToPoints(ch_lat_from, ch_lat_to,ch_long_from, ch_long_to, False)
        if len(lst_vertices):
            vertsToFaces(lst_vertices, lst_colors, row_count, column_count)
    else:
        lst_vertices, lst_colors, row_count, column_count = sectionToPoints(ch_lat_from, ch_lat_to,ch_long_from, ch_long_to, True)

if projection_type == 1:
    # Rectangular projection.
    if `ch("tgl_make_faces")` == True:
        # Rectangular projection.
        lst_vertices, lst_colors, row_count, column_count = rowsToPoints(True)
        if len(lst_vertices):
            mapPointsToFaces(row_count,column_count)
    else:
        lst_vertices, lst_colors, row_count, column_count = rowsToPoints(True)

elif projection_type == 2:
    # No projection, just make points with associated height data.
    # Use this generation method when you want to activate the companion AttributeWrange for VEX based face generation.
    # Uses controls from the LDEM_256 or better.
    areaToPoints(filename, ch_lat_from, ch_lat_to, ch_long_from, ch_long_to, True)
